using System;
using System.Collections.Generic;
using ShawarmaTycoon.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Touch-first restaurant layout editor. Gameplay is paused while it is open,
    /// but UI and pointer input keep running on unscaled time.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class BuildModeController : MonoBehaviour
    {
        private const float GridSize = 0.25f;
        private const float RayDistance = 250f;
        private readonly RaycastHit[] rayHits = new RaycastHit[96];
        private readonly Collider[] overlapHits = new Collider[128];
        private readonly List<RaycastResult> uiHits = new();

        private Camera worldCamera;
        private MobilePlayerController playerMotor;
        private TouchJoystick joystick;
        private DioramaWalkableRegistry walkableRegistry;
        private BuildModeHUD hud;
        private PlaceableObject selected;
        private Vector3 dragOffset;
        private float groundOffset;
        private bool dragging;
        private bool placementValid;
        private float previousTimeScale = 1f;
        private bool playerWasEnabled;
        private GameObject indicator;
        private Renderer[] indicatorBars;
        private string message = "Bir eşyaya dokunup sürükle";

        public bool IsActive { get; private set; }
        public PlaceableObject Selected => selected;
        public bool PlacementValid => placementValid;
        public string Message => message;

        /// <summary>Read-only placement query used by diagnostics and runtime tests.</summary>
        public bool CanPlace(PlaceableObject item) => IsValidPlacement(item);

        public void Configure(
            Camera camera, MobilePlayerController motor, TouchJoystick touchJoystick,
            DioramaWalkableRegistry registry, BuildModeHUD buildHud)
        {
            worldCamera = camera;
            playerMotor = motor;
            joystick = touchJoystick;
            walkableRegistry = registry;
            hud = buildHud;
            hud?.Bind(this);
            BuildIndicator();
        }

        public void ToggleBuildMode() => SetBuildMode(!IsActive);

        public void SetBuildMode(bool active)
        {
            if (IsActive == active) return;
            if (active && (worldCamera == null || walkableRegistry == null))
            {
                message = "İnşa modu hazır değil";
                AudioDirector.Play(GameSfx.Error, 0.6f);
                return;
            }

            IsActive = active;
            if (active)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                if (playerMotor != null)
                {
                    playerWasEnabled = playerMotor.enabled;
                    playerMotor.enabled = true;
                    playerMotor.SetBuildModeMovement(true);
                }
                joystick?.SetInputEnabled(true);
                joystick?.SetBuildMode(true);
                message = "Sol alttan yürü • Eşyaya dokunup sürükle";
                AudioDirector.Play(GameSfx.Pickup, 0.65f);
            }
            else
            {
                FinishDrag();
                Select(null);
                Time.timeScale = previousTimeScale;
                if (playerMotor != null)
                {
                    playerMotor.SetBuildModeMovement(false);
                    playerMotor.enabled = playerWasEnabled;
                }
                joystick?.SetBuildMode(false);
                joystick?.SetInputEnabled(true);
                GameProgress.FlushNow();
            }
            hud?.RefreshImmediately();
        }

        private void OnDisable()
        {
            if (!IsActive) return;
            IsActive = false;
            Time.timeScale = previousTimeScale;
            if (playerMotor != null)
            {
                playerMotor.SetBuildModeMovement(false);
                playerMotor.enabled = playerWasEnabled;
            }
            joystick?.SetBuildMode(false);
            joystick?.SetInputEnabled(true);
        }

        private void Update()
        {
            if (!IsActive) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    SetBuildMode(false);
                    return;
                }
                if (keyboard.rKey.wasPressedThisFrame) RotateSelected();
            }

            Pointer pointer = Pointer.current;
            if (pointer == null) return;
            Vector2 screenPosition = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                if (joystick != null &&
                    joystick.ClaimsBuildModePointer(screenPosition, pointer is Touchscreen)) return;
                if (IsPointerOverBlockingUI(screenPosition)) return;
                BeginPointer(screenPosition);
            }
            else if (dragging && pointer.press.isPressed)
                ContinueDrag(screenPosition);

            if (dragging && pointer.press.wasReleasedThisFrame)
                FinishDrag();

            UpdateIndicator();
        }

        /// <summary>The joystick catcher is transparent UI, but it must not hide furniture rays.</summary>
        private bool IsPointerOverBlockingUI(Vector2 screenPosition)
        {
            EventSystem events = EventSystem.current;
            if (events == null) return false;

            uiHits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = screenPosition }, uiHits);
            for (int i = 0; i < uiHits.Count; i++)
            {
                GameObject hit = uiHits[i].gameObject;
                if (hit != null && hit.GetComponentInParent<TouchJoystick>() != null) continue;
                return true;
            }
            return false;
        }

        private void BeginPointer(Vector2 screenPosition)
        {
            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            int count = Physics.RaycastNonAlloc(ray, rayHits, RayDistance, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(rayHits, 0, count, RaycastHitDistanceComparer.Instance);

            PlaceableObject target = null;
            for (int i = 0; i < count; i++)
            {
                target = rayHits[i].collider.GetComponentInParent<PlaceableObject>();
                if (target != null && target.IsSelectable) break;
                target = null;
            }

            if (target == null)
            {
                Select(null);
                message = "Taşımak için bir eşya seç";
                return;
            }
            if (!target.CanMove)
            {
                Select(target);
                message = "Bu masa kullanımdayken taşınamaz";
                AudioDirector.Play(GameSfx.Error, 0.55f);
                return;
            }

            Select(target);
            selected.CaptureCommittedState();
            Plane plane = new(Vector3.up, selected.transform.position);
            if (!plane.Raycast(ray, out float enter)) return;

            Vector3 point = ray.GetPoint(enter);
            dragOffset = selected.transform.position - point;
            if (!walkableRegistry.TryGetGroundHeight(selected.transform.position, out float floorY))
                floorY = selected.transform.position.y;
            groundOffset = selected.transform.position.y - floorY;
            dragging = true;
            ContinueDrag(screenPosition);
        }

        private void ContinueDrag(Vector2 screenPosition)
        {
            if (selected == null) return;
            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            Plane plane = new(Vector3.up, selected.transform.position);
            if (!plane.Raycast(ray, out float enter)) return;

            Vector3 desired = ray.GetPoint(enter) + dragOffset;
            desired.x = Mathf.Round(desired.x / GridSize) * GridSize;
            desired.z = Mathf.Round(desired.z / GridSize) * GridSize;
            if (walkableRegistry.TryGetGroundHeight(desired, out float floorY))
                desired.y = floorY + groundOffset;
            else
                desired.y = selected.transform.position.y;

            selected.MoveWorld(desired);
            placementValid = IsValidPlacement(selected);
            message = placementValid ? "Bırakınca yerleşecek" : "Buraya yerleştirilemez";
        }

        private void FinishDrag()
        {
            if (!dragging || selected == null) return;
            dragging = false;
            placementValid = IsValidPlacement(selected);
            if (!placementValid)
            {
                selected.RevertToCommitted();
                placementValid = true;
                message = "Geçersiz konum geri alındı";
                AudioDirector.Play(GameSfx.Error, 0.55f);
            }
            else
            {
                selected.Commit();
                message = selected.DisplayName + " yerleştirildi";
                AudioDirector.Play(GameSfx.Pickup, 0.65f);
            }
        }

        public void RotateSelected()
        {
            if (!IsActive || selected == null || !selected.CanMove) return;
            FinishDrag();
            selected.CaptureCommittedState();
            selected.RotateQuarterTurn();
            placementValid = IsValidPlacement(selected);
            if (!placementValid)
            {
                selected.RevertToCommitted();
                placementValid = true;
                message = "Döndürmek için yeterli alan yok";
                AudioDirector.Play(GameSfx.Error, 0.55f);
                return;
            }
            selected.Commit();
            message = selected.DisplayName + " döndürüldü";
            AudioDirector.Play(GameSfx.Pickup, 0.6f);
        }

        public void ResetSelected()
        {
            if (!IsActive || selected == null || !selected.CanMove) return;
            FinishDrag();
            selected.CaptureCommittedState();
            selected.ResetToDefault();
            placementValid = IsValidPlacement(selected);
            if (!placementValid)
            {
                selected.RevertToCommitted();
                placementValid = true;
                message = "Varsayılan konum şu anda dolu";
                AudioDirector.Play(GameSfx.Error, 0.55f);
                return;
            }
            selected.Commit();
            message = selected.DisplayName + " sıfırlandı";
            AudioDirector.Play(GameSfx.Pickup, 0.6f);
        }

        private void Select(PlaceableObject target)
        {
            selected = target;
            dragging = false;
            placementValid = selected == null || IsValidPlacement(selected);
            if (indicator != null) indicator.SetActive(selected != null && IsActive);
            hud?.RefreshImmediately();
        }

        private bool IsValidPlacement(PlaceableObject item)
        {
            if (item == null || walkableRegistry == null) return false;
            Bounds bounds = item.FootprintBounds;
            if (!walkableRegistry.ContainsBoundsXZ(bounds, 0.08f)) return false;
            bool movingConveyor = item.GetComponent<ConveyorLink>() != null;
            bool movingStation = item.GetComponent<ItemStation>() != null;

            Vector3 half = bounds.extents;
            half.x = Mathf.Max(0.12f, half.x - 0.08f);
            half.z = Mathf.Max(0.12f, half.z - 0.08f);
            half.y = Mathf.Max(0.08f, half.y - 0.03f);
            int count = Physics.OverlapBoxNonAlloc(
                bounds.center, half, overlapHits, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider hit = overlapHits[i];
                if (hit == null || !hit.enabled || hit.transform.IsChildOf(item.transform)) continue;
                if (hit.GetComponentInParent<DioramaWalkableSurface>() != null) continue;
                if (hit.GetComponentInParent<MobilePlayerController>() != null) continue;
                if (hit.GetComponentInParent<CustomerAgent>() != null) continue;
                if (hit.GetComponentInParent<WorkerAgent>() != null) continue;
                if (movingConveyor && hit.GetComponentInParent<ItemStation>() != null) continue;
                if (movingStation && hit.GetComponentInParent<ConveyorLink>() != null) continue;
                return false;
            }
            return true;
        }

        private void BuildIndicator()
        {
            indicator = new GameObject("Build Placement Indicator");
            indicator.transform.SetParent(transform, false);
            indicatorBars = new Renderer[4];
            for (int i = 0; i < indicatorBars.Length; i++)
            {
                GameObject bar = PrototypeVisuals.CreatePrimitive(
                    "Edge " + i, PrimitiveType.Cube, indicator.transform,
                    Vector3.zero, Vector3.one, PrototypeVisuals.Green);
                indicatorBars[i] = bar.GetComponent<Renderer>();
            }
            indicator.SetActive(false);
        }

        private void UpdateIndicator()
        {
            if (indicator == null || selected == null)
            {
                if (indicator != null) indicator.SetActive(false);
                return;
            }

            indicator.SetActive(true);
            Bounds bounds = selected.FootprintBounds;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.035f;
            float width = 0.075f * pulse;
            float y = bounds.min.y + 0.045f;
            indicator.transform.position = Vector3.zero;
            indicator.transform.rotation = Quaternion.identity;

            SetBar(0, new Vector3(bounds.center.x, y, bounds.min.z), new Vector3(bounds.size.x, 0.035f, width));
            SetBar(1, new Vector3(bounds.center.x, y, bounds.max.z), new Vector3(bounds.size.x, 0.035f, width));
            SetBar(2, new Vector3(bounds.min.x, y, bounds.center.z), new Vector3(width, 0.035f, bounds.size.z));
            SetBar(3, new Vector3(bounds.max.x, y, bounds.center.z), new Vector3(width, 0.035f, bounds.size.z));

            Color color = placementValid ? PrototypeVisuals.Green : PrototypeVisuals.Red;
            Material material = PrototypeVisuals.Material(color);
            for (int i = 0; i < indicatorBars.Length; i++)
                indicatorBars[i].sharedMaterial = material;
        }

        private void SetBar(int index, Vector3 position, Vector3 scale)
        {
            Transform bar = indicatorBars[index].transform;
            bar.position = position;
            bar.rotation = Quaternion.identity;
            bar.localScale = scale;
        }

        private sealed class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
