using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Marks a runtime-built restaurant prop as movable and owns its persistent
    /// local transform. Position values are stored as millimetres so the existing
    /// integer save repository can keep layouts without a parallel save format.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaceableObject : MonoBehaviour
    {
        private const float UnitsToSave = 1000f;
        private const string ProxyName = "Build Selection Proxy";

        [SerializeField] private string stableId;
        [SerializeField] private string displayName;

        private Vector3 defaultLocalPosition;
        private Quaternion defaultLocalRotation;
        private Vector3 committedLocalPosition;
        private Quaternion committedLocalRotation;
        private BoxCollider selectionProxy;
        private bool initialized;

        public string StableId => stableId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        public bool IsSelectable
        {
            get
            {
                if (!gameObject.activeInHierarchy) return false;
                Renderer[] visible = GetComponentsInChildren<Renderer>(false);
                for (int i = 0; i < visible.Length; i++)
                    if (visible[i] != null && visible[i].enabled) return true;
                return false;
            }
        }
        public Bounds FootprintBounds
        {
            get
            {
                EnsureInitialized();
                Physics.SyncTransforms();
                return selectionProxy != null
                    ? selectionProxy.bounds
                    : new Bounds(transform.position, Vector3.one * 0.5f);
            }
        }

        public bool CanMove
        {
            get
            {
                CustomerTable table = GetComponent<CustomerTable>();
                return table == null || !table.IsReserved;
            }
        }

        public void Configure(string id, string label)
        {
            stableId = id;
            displayName = label;
        }

        private void Start() => EnsureInitialized();

        public void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            defaultLocalPosition = transform.localPosition;
            defaultLocalRotation = transform.localRotation;
            LoadSavedTransform();
            committedLocalPosition = transform.localPosition;
            committedLocalRotation = transform.localRotation;
            BuildSelectionProxy();
        }

        public void CaptureCommittedState()
        {
            EnsureInitialized();
            committedLocalPosition = transform.localPosition;
            committedLocalRotation = transform.localRotation;
        }

        public void RevertToCommitted()
        {
            EnsureInitialized();
            transform.localPosition = committedLocalPosition;
            transform.localRotation = committedLocalRotation;
            Physics.SyncTransforms();
        }

        public void MoveWorld(Vector3 worldPosition)
        {
            EnsureInitialized();
            transform.position = worldPosition;
            Physics.SyncTransforms();
        }

        public void RotateQuarterTurn()
        {
            EnsureInitialized();
            transform.Rotate(Vector3.up, 90f, Space.World);
            Physics.SyncTransforms();
        }

        public void ResetToDefault()
        {
            EnsureInitialized();
            transform.localPosition = defaultLocalPosition;
            transform.localRotation = defaultLocalRotation;
            Physics.SyncTransforms();
        }

        public void Commit()
        {
            EnsureInitialized();
            committedLocalPosition = transform.localPosition;
            committedLocalRotation = transform.localRotation;
            if (string.IsNullOrWhiteSpace(stableId)) return;

            string prefix = "layout." + stableId + ".";
            GameProgress.SetInt(prefix + "placed", 1);
            GameProgress.SetInt(prefix + "x", Mathf.RoundToInt(committedLocalPosition.x * UnitsToSave));
            GameProgress.SetInt(prefix + "z", Mathf.RoundToInt(committedLocalPosition.z * UnitsToSave));
            GameProgress.SetInt(prefix + "yaw", Mathf.RoundToInt(NormalizeYaw(committedLocalRotation.eulerAngles.y)));
            GameProgress.FlushNow();
        }

        private void LoadSavedTransform()
        {
            if (string.IsNullOrWhiteSpace(stableId)) return;
            string prefix = "layout." + stableId + ".";
            if (GameProgress.GetInt(prefix + "placed") != 1) return;

            Vector3 local = defaultLocalPosition;
            local.x = GameProgress.GetInt(prefix + "x", Mathf.RoundToInt(local.x * UnitsToSave)) / UnitsToSave;
            local.z = GameProgress.GetInt(prefix + "z", Mathf.RoundToInt(local.z * UnitsToSave)) / UnitsToSave;
            transform.localPosition = local;
            transform.localRotation = Quaternion.Euler(0f,
                GameProgress.GetInt(prefix + "yaw", Mathf.RoundToInt(defaultLocalRotation.eulerAngles.y)), 0f);
        }

        private void BuildSelectionProxy()
        {
            Transform existing = transform.Find(ProxyName);
            GameObject proxyObject;
            if (existing != null)
            {
                proxyObject = existing.gameObject;
                selectionProxy = proxyObject.GetComponent<BoxCollider>();
            }
            else
            {
                proxyObject = new GameObject(ProxyName);
                proxyObject.transform.SetParent(transform, false);
                selectionProxy = proxyObject.AddComponent<BoxCollider>();
            }

            selectionProxy.isTrigger = true;
            Bounds localBounds = MeasureLocalVisualBounds();
            selectionProxy.center = localBounds.center;
            Vector3 size = localBounds.size;
            size.x = Mathf.Max(0.5f, size.x);
            size.y = Mathf.Max(0.25f, size.y);
            size.z = Mathf.Max(0.5f, size.z);
            selectionProxy.size = size;
        }

        private Bounds MeasureLocalVisualBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds localBounds = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.GetComponent<TextMesh>() != null ||
                    renderer.transform.name == ProxyName)
                    continue;

                Bounds world = renderer.bounds;
                Vector3 min = world.min;
                Vector3 max = world.max;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = new(
                        (corner & 1) == 0 ? min.x : max.x,
                        (corner & 2) == 0 ? min.y : max.y,
                        (corner & 4) == 0 ? min.z : max.z);
                    Vector3 local = transform.InverseTransformPoint(point);
                    if (!found)
                    {
                        localBounds = new Bounds(local, Vector3.zero);
                        found = true;
                    }
                    else localBounds.Encapsulate(local);
                }
            }

            return found ? localBounds : new Bounds(Vector3.up * 0.5f, Vector3.one);
        }

        private static float NormalizeYaw(float yaw)
        {
            yaw %= 360f;
            return yaw < 0f ? yaw + 360f : yaw;
        }
    }
}
