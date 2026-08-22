using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class ItemStation : MonoBehaviour
    {
        [Header("Runtime configuration")]
        [SerializeField] private StationMode mode;
        [SerializeField] private ItemType inputType;
        [SerializeField] private ItemType outputType;
        [SerializeField, Min(1)] private int inputCapacity = 8;
        [SerializeField, Min(1)] private int outputCapacity = 8;
        [SerializeField, Min(0.1f)] private float processDuration = 2f;
        [SerializeField, Min(0.1f)] private float sourceInterval = 0.8f;
        [SerializeField, Min(0.5f)] private float interactionRadius = 1.55f;
        [SerializeField, Min(0.02f)] private float transferInterval = 0.13f;

        private readonly List<GameObject> inputVisuals = new();
        private readonly List<GameObject> outputVisuals = new();

        private Transform player;
        private CarryInventory inventory;
        private Transform inputRoot;
        private Transform outputRoot;
        private TextMesh statusLabel;
        private TextMesh maxLabel;
        private GameObject warningBadge;
        private Renderer warningBadgePanel;
        private readonly Renderer[] statusLights = new Renderer[4];
        private Transform statusLightsRoot;
        private int statusLightSignature = int.MinValue;
        private int inputCount;
        private int outputCount;
        private float processTimer;
        private float sourceTimer;
        private float transferTimer;

        public StationMode Mode => mode;
        public ItemType InputType => inputType;
        public ItemType OutputType => outputType;
        public int InputCount => inputCount;
        public int OutputCount => outputCount;
        public float ProcessProgress => processDuration <= 0f ? 0f : Mathf.Clamp01(processTimer / processDuration);
        private int EffectiveInputCapacity => inputCapacity + HumanResourcesSystem.WorkerCapacityBonus;
        private int EffectiveOutputCapacity => outputCapacity + HumanResourcesSystem.WorkerCapacityBonus;

        public bool CanReceiveInput => inputCount < EffectiveInputCapacity;
        public bool HasOutput => outputCount > 0;

        private float repairNeeded;

        /// <summary>
        /// A broken station takes deliveries and hands out what it already made,
        /// but will not process. Repairing it is a job for whoever is standing in
        /// the shop, which is the point: a fully automated kitchen otherwise never
        /// needs the player again.
        /// </summary>
        public bool IsBroken => repairNeeded > 0f;

        /// <summary>Seconds of standing at it still needed, as a 0..1 fraction.</summary>
        public float RepairProgress => breakSeverity <= 0f
            ? 0f
            : 1f - Mathf.Clamp01(repairNeeded / breakSeverity);

        private float breakSeverity;

        public void Break(float secondsToRepair)
        {
            if (mode != StationMode.Processor || IsBroken) return;
            breakSeverity = Mathf.Max(0.5f, secondsToRepair);
            repairNeeded = breakSeverity;
            UpdateMaxIndicator();
        }

        /// <summary>Works on the repair; true once it is fixed.</summary>
        public bool Repair(float seconds)
        {
            if (!IsBroken) return true;
            repairNeeded -= Mathf.Max(0f, seconds);
            if (repairNeeded > 0f) return false;

            repairNeeded = 0f;
            breakSeverity = 0f;
            AudioDirector.Play(GameSfx.Unlock, 0.7f);
            UpdateMaxIndicator();
            return true;
        }

        /// <summary>
        /// Whether a hand delivery of <see cref="InputType"/> would be taken. The
        /// service counter is the end of the line and holds what it is given as
        /// output, so its own limit is the one that matters there.
        /// </summary>
        public bool CanAcceptDelivery => mode == StationMode.Service
            ? outputCount < EffectiveOutputCapacity
            : CanReceiveInput;

        /// <summary>
        /// Finished goods are piled up with nowhere to go, so the station has
        /// stopped. Standing here will not restart it - the pile has to be moved.
        /// </summary>
        public bool OutputIsFull => outputCount >= EffectiveOutputCapacity;

        public void SetWorldLabelVisible(bool visible)
        {
            if (statusLabel != null) statusLabel.gameObject.SetActive(visible);
        }

        private string outputBatchAsset;
        private int outputBatchSize = 4;
        private Vector3 outputBatchTargetSize = Vector3.one * 0.4f;
        private float outputBatchHeight = 0.4f;
        private float visualItemScale = 0.85f;
        private int outputGridColumns;
        private float outputGridSpacingX = 0.24f;
        private float outputGridSpacingZ = 0.18f;

        /// <summary>
        /// Shows the output pile as authored full trays: one model per
        /// <paramref name="itemsPerBatch"/> units, with any remainder still drawn
        /// as loose portions so the exact count stays readable.
        /// </summary>
        public void SetOutputBatchVisual(
            string assetId, int itemsPerBatch, Vector3 targetSize, float stackHeight)
        {
            outputBatchAsset = assetId;
            outputBatchSize = Mathf.Max(1, itemsPerBatch);
            outputBatchTargetSize = targetSize;
            outputBatchHeight = Mathf.Max(0.05f, stackHeight);
            RefreshVisuals();
        }

        /// <summary>Station-only display scale; carried food keeps its own size.</summary>
        public void SetVisualItemScale(float scale)
        {
            visualItemScale = Mathf.Clamp(scale, 0.5f, 1.6f);
            RefreshVisuals();
        }

        /// <summary>Spreads loose output across a shelf instead of making a tower.</summary>
        public void SetOutputGrid(int columns, float spacingX, float spacingZ)
        {
            outputGridColumns = Mathf.Max(0, columns);
            outputGridSpacingX = Mathf.Max(0.05f, spacingX);
            outputGridSpacingZ = Mathf.Max(0.05f, spacingZ);
            RefreshVisuals();
        }

        public void SetVisualLayout(Vector3 inputLocalPosition, Vector3 outputLocalPosition, float maxLabelHeight)
        {
            if (inputRoot != null) inputRoot.localPosition = inputLocalPosition;
            if (outputRoot != null) outputRoot.localPosition = outputLocalPosition;
            if (statusLightsRoot != null)
            {
                Vector3 display = mode == StationMode.Processor
                    ? Vector3.Lerp(inputLocalPosition, outputLocalPosition, 0.5f)
                    : outputLocalPosition;
                statusLightsRoot.localPosition = new Vector3(
                    display.x, display.y + 0.11f, display.z - 0.23f);
            }
            if (warningBadge != null)
                warningBadge.transform.localPosition = new Vector3(0f, Mathf.Max(0.5f, maxLabelHeight), 0f);
        }

        public void Configure(
            string displayName,
            StationMode stationMode,
            ItemType stationInput,
            ItemType stationOutput,
            Transform playerTransform,
            CarryInventory playerInventory,
            float processingSeconds,
            int stationInputCapacity = 8,
            int stationOutputCapacity = 8,
            float refillSeconds = 0.8f)
        {
            name = displayName;
            mode = stationMode;
            inputType = stationInput;
            outputType = stationOutput;
            player = playerTransform;
            inventory = playerInventory;
            processDuration = Mathf.Max(0.1f, processingSeconds);
            inputCapacity = Mathf.Max(1, stationInputCapacity);
            outputCapacity = Mathf.Max(1, stationOutputCapacity);
            sourceInterval = Mathf.Max(0.1f, refillSeconds);

            inputRoot = CreateRoot("Input Stack", new Vector3(-0.48f, 0.78f, 0f));
            outputRoot = CreateRoot("Output Stack", new Vector3(0.48f, 0.78f, 0f));
            statusLabel = PrototypeVisuals.CreateLabel(displayName, transform, new Vector3(0f, 1.55f, 0f), 0.13f);
            BuildWarningBadge();
            BuildStatusLights();

            if (mode == StationMode.Source)
                outputCount = outputCapacity;

            HumanResourcesSystem.CapacityChanged += RefreshVisuals;
            RefreshVisuals();
        }

        private void OnDestroy()
        {
            HumanResourcesSystem.CapacityChanged -= RefreshVisuals;
        }

        private Transform CreateRoot(string rootName, Vector3 localPosition)
        {
            GameObject root = new(rootName);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = localPosition;
            return root.transform;
        }

        private void BuildWarningBadge()
        {
            warningBadge = new GameObject("İstasyon Durum Kartı");
            warningBadge.transform.SetParent(transform, false);
            warningBadge.transform.localPosition = new Vector3(0f, 2.05f, 0f);
            warningBadge.transform.localEulerAngles = new Vector3(55f, 0f, 0f);

            PrototypeVisuals.CreatePrimitive(
                "Gölge", PrimitiveType.Cube, warningBadge.transform,
                new Vector3(0.035f, -0.035f, 0.045f), new Vector3(1.14f, 0.34f, 0.055f),
                new Color(0.25f, 0.14f, 0.10f));
            GameObject panel = PrototypeVisuals.CreatePrimitive(
                "Kart", PrimitiveType.Cube, warningBadge.transform,
                Vector3.zero, new Vector3(1.08f, 0.30f, 0.060f),
                new Color(1f, 0.78f, 0.28f));
            warningBadgePanel = panel.GetComponent<Renderer>();

            GameObject labelObject = new("Durum");
            labelObject.transform.SetParent(warningBadge.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.045f);
            maxLabel = labelObject.AddComponent<TextMesh>();
            maxLabel.anchor = TextAnchor.MiddleCenter;
            maxLabel.alignment = TextAlignment.Center;
            maxLabel.font = UI.UITheme.DisplayFont;
            maxLabel.fontSize = 64;
            maxLabel.characterSize = 0.042f;
            maxLabel.fontStyle = FontStyle.Bold;
            maxLabel.color = new Color(0.25f, 0.14f, 0.10f);
            Renderer textRenderer = maxLabel.GetComponent<Renderer>();
            if (textRenderer != null && maxLabel.font != null)
                textRenderer.sharedMaterial = maxLabel.font.material;
            warningBadge.SetActive(false);
        }

        /// <summary>
        /// Four tiny counter lights make the kitchen readable at camera distance:
        /// amber is stock waiting, green is work in progress, teal is ready food,
        /// and red is a fault. They replace another row of floating numbers.
        /// </summary>
        private void BuildStatusLights()
        {
            GameObject root = new("Durum Lambaları");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, 1.12f, -0.62f);
            statusLightsRoot = root.transform;

            for (int i = 0; i < statusLights.Length; i++)
            {
                GameObject light = PrototypeVisuals.CreatePrimitive(
                    "Lamba " + (i + 1), PrimitiveType.Sphere, root.transform,
                    new Vector3((i - 1.5f) * 0.17f, 0f, 0f), Vector3.one * 0.115f,
                    new Color(0.27f, 0.25f, 0.23f));
                PaymentFlyer.DisablePhysicsAndShadows(light);
                statusLights[i] = light.GetComponent<Renderer>();
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (mode == StationMode.Source)
                UpdateSource();
            else if (mode == StationMode.Processor)
                UpdateProcessor();

            transferTimer -= Time.deltaTime;
            if (player != null && inventory != null &&
                Vector3.SqrMagnitude(player.position - transform.position) <= interactionRadius * interactionRadius &&
                transferTimer <= 0f)
            {
                if (TryInteract())
                    transferTimer = transferInterval;
            }
            RefreshStatusLights();
        }

        private void UpdateSource()
        {
            if (outputCount >= EffectiveOutputCapacity) return;
            sourceTimer += Time.deltaTime;
            if (sourceTimer < sourceInterval) return;
            sourceTimer = 0f;
            outputCount++;
            RefreshVisuals();
        }

        private void UpdateProcessor()
        {
            if (IsBroken || inputCount <= 0 || outputCount >= EffectiveOutputCapacity)
            {
                processTimer = 0f;
                return;
            }

            // A fed station works on its own. Its visible progression is the belt
            // that feeds it; shared management automation handles later speedups.
            processTimer += Time.deltaTime;
            if (processTimer < processDuration) return;

            processTimer = 0f;
            inputCount--;
            outputCount++;
            AudioDirector.Play(GameSfx.Cook, 0.7f);
            RefreshVisuals();
        }

        private bool TryInteract()
        {
            if (mode == StationMode.Source)
                return TryGiveOutputToPlayer();

            if (mode == StationMode.Service)
            {
                if (inventory.HeldType == inputType && outputCount < EffectiveOutputCapacity && inventory.TryRemove(inputType))
                {
                    outputCount++;
                    ItemTransferArc.Send(
                        inputType, player.position + Vector3.up * 1.25f,
                        outputRoot != null ? outputRoot.position : transform.position + Vector3.up);
                    AudioDirector.Play(GameSfx.Drop, 0.7f);
                    RefreshVisuals();
                    return true;
                }
                return false;
            }

            if (inventory.HeldType == inputType && inputCount < EffectiveInputCapacity && inventory.TryRemove(inputType))
            {
                inputCount++;
                ItemTransferArc.Send(
                    inputType, player.position + Vector3.up * 1.25f,
                    inputRoot != null ? inputRoot.position : transform.position + Vector3.up);
                AudioDirector.Play(GameSfx.Drop, 0.7f);
                RefreshVisuals();
                return true;
            }

            return TryGiveOutputToPlayer();
        }

        private bool TryGiveOutputToPlayer()
        {
            if (outputCount <= 0 || !inventory.CanAccept(outputType)) return false;
            if (!inventory.TryAdd(outputType)) return false;
            outputCount--;
            ItemTransferArc.Send(
                outputType, outputRoot != null ? outputRoot.position : transform.position + Vector3.up,
                player.position + Vector3.up * 1.25f);
            AudioDirector.Play(GameSfx.Pickup, 0.7f);
            RefreshVisuals();
            return true;
        }

        /// <summary>
        /// Puts stock on the counter before the shop opens. Without it the first
        /// ninety seconds of a new game earn nothing at all: the queue is already
        /// at the door while the first tray of meat is still working its way down
        /// a cold line, and there is nothing to sell until it arrives.
        /// </summary>
        public void Prime(int units)
        {
            if (units <= 0) return;
            outputCount = Mathf.Clamp(outputCount + units, 0, EffectiveOutputCapacity);
            RefreshVisuals();
        }

        public bool TryTakeServiceItem()
        {
            if (mode != StationMode.Service || outputCount <= 0) return false;
            outputCount--;
            RefreshVisuals();
            return true;
        }

        /// <summary>
        /// Hands one unit to a customer, whatever kind of station this is. The
        /// serving counter is a Service station but the fridge and the dessert
        /// oven are not, and a customer buying a drink does not care which.
        /// </summary>
        public bool TryTakeForCustomer()
        {
            if (outputCount <= 0) return false;
            outputCount--;
            RefreshVisuals();
            return true;
        }

        private string emptyWarning;

        /// <summary>
        /// Turns the station's warning into a run-dry sign rather than a full-up
        /// one. The fridge is the only thing in the shop that can be empty and
        /// stay empty until someone walks stock over to it, so it is the only one
        /// that has to say so.
        /// </summary>
        public void SetEmptyWarning(string text)
        {
            emptyWarning = text;
            UpdateMaxIndicator();
        }

        /// <summary>
        /// Lit only when finished goods have nowhere to go, which is the one state
        /// waiting will not clear. A full input tray used to light it too, and now
        /// that a belt keeps the tray topped up that is the normal running state -
        /// the warning would have been on almost permanently, meaning nothing.
        /// </summary>
        private void UpdateMaxIndicator()
        {
            if (maxLabel == null) return;

            if (IsBroken)
            {
                maxLabel.text = "ARIZA";
                SetWarningBadge(true, PrototypeVisuals.Red);
                return;
            }

            if (!string.IsNullOrEmpty(emptyWarning))
            {
                maxLabel.text = emptyWarning;
                SetWarningBadge(outputCount <= 0, PrototypeVisuals.Red);
                return;
            }

            maxLabel.text = "DOLU";
            SetWarningBadge(
                mode != StationMode.Source && outputCount >= EffectiveOutputCapacity,
                new Color(1f, 0.78f, 0.28f));
        }

        private void RefreshStatusLights()
        {
            if (statusLights[0] == null) return;

            int progressSegments = mode == StationMode.Processor
                ? Mathf.Clamp(Mathf.CeilToInt(ProcessProgress * 2f), 0, 2)
                : 0;
            int signature = ((int)mode << 24) | (IsBroken ? 1 << 23 : 0) |
                (Mathf.Min(inputCount, 31) << 12) | (Mathf.Min(outputCount, 31) << 4) |
                progressSegments;
            if (signature == statusLightSignature) return;
            statusLightSignature = signature;

            Color off = new(0.27f, 0.25f, 0.23f);
            Color amber = new(1f, 0.69f, 0.20f);
            Color green = new(0.34f, 0.84f, 0.46f);
            Color teal = new(0.24f, 0.78f, 0.78f);
            Color red = new(0.94f, 0.29f, 0.24f);

            Color[] colors = { off, off, off, off };
            if (IsBroken)
            {
                for (int i = 0; i < colors.Length; i++) colors[i] = red;
            }
            else if (mode == StationMode.Processor)
            {
                colors[0] = inputCount > 0 ? amber : off;
                colors[1] = progressSegments >= 1 ? green : off;
                colors[2] = progressSegments >= 2 ? green : off;
                colors[3] = outputCount > 0 ? (OutputIsFull ? amber : teal) : off;
            }
            else
            {
                int capacity = EffectiveOutputCapacity;
                int lit = Mathf.Clamp(Mathf.CeilToInt(outputCount / (float)Mathf.Max(1, capacity) * 4f), 0, 4);
                for (int i = 0; i < lit; i++) colors[i] = mode == StationMode.Service ? teal : green;
            }

            for (int i = 0; i < statusLights.Length; i++)
                if (statusLights[i] != null)
                    statusLights[i].sharedMaterial = PrototypeVisuals.Material(colors[i]);
        }

        private void SetWarningBadge(bool visible, Color panelColor)
        {
            if (warningBadge != null) warningBadge.SetActive(visible);
            if (warningBadgePanel != null)
                warningBadgePanel.sharedMaterial = PrototypeVisuals.Material(panelColor);
        }

        public bool TryReceiveFromConveyor(ItemType item)
        {
            if (mode == StationMode.Source || item != inputType)
                return false;

            if (mode == StationMode.Service)
            {
                if (outputCount >= EffectiveOutputCapacity) return false;
                outputCount++;
                RefreshVisuals();
                return true;
            }

            if (inputCount >= EffectiveInputCapacity) return false;

            inputCount++;
            RefreshVisuals();
            return true;
        }

        public bool TryTakeOutputForConveyor(out ItemType item)
        {
            item = ItemType.None;
            if (mode == StationMode.Service || outputCount <= 0)
                return false;

            item = outputType;
            outputCount--;
            RefreshVisuals();
            return true;
        }

        public void ReturnOutputFromConveyor(ItemType item)
        {
            if (mode != StationMode.Service && item == outputType && outputCount < EffectiveOutputCapacity)
            {
                outputCount++;
                RefreshVisuals();
            }
        }

        private void RefreshVisuals()
        {
            ClearVisuals(inputVisuals);
            ClearVisuals(outputVisuals);

            int shownInput = Mathf.Min(inputCount, 6);
            int shownOutput = Mathf.Min(outputCount, 8);

            if (inputType != ItemType.None && inputRoot != null)
            {
                float step = PrototypeVisuals.StackStep(inputType, visualItemScale);
                for (int i = 0; i < shownInput; i++)
                    inputVisuals.Add(RentItemVisual(
                        inputType, inputRoot, Vector3.up * (i * step), visualItemScale));
            }

            ItemType visibleOutputType = mode == StationMode.Service ? inputType : outputType;
            if (visibleOutputType != ItemType.None && outputRoot != null)
            {
                int loose = shownOutput;
                float y = 0f;

                if (!string.IsNullOrEmpty(outputBatchAsset))
                {
                    int batches = loose / outputBatchSize;
                    for (int b = 0; b < batches; b++)
                    {
                        string assetId = outputBatchAsset;
                        GameObject tray = GameplayObjectPool.Rent(
                            $"station.batch.{assetId}", outputRoot,
                            () => MeshyVisuals.TryAttach(
                                outputRoot, assetId, outputBatchTargetSize, Vector3.zero, Vector3.zero));
                        if (tray == null) break;      // model missing: fall back to portions
                        tray.transform.localPosition = Vector3.up * y;
                        outputVisuals.Add(tray);
                        y += outputBatchHeight;
                        loose -= outputBatchSize;
                    }
                }

                float step = PrototypeVisuals.StackStep(visibleOutputType, visualItemScale);
                for (int i = 0; i < loose; i++)
                {
                    Vector3 loosePosition;
                    if (outputGridColumns > 0)
                    {
                        int column = i % outputGridColumns;
                        int row = i / outputGridColumns;
                        float centre = (outputGridColumns - 1) * 0.5f;
                        loosePosition = new Vector3(
                            (column - centre) * outputGridSpacingX,
                            y + row * 0.035f,
                            row * outputGridSpacingZ);
                    }
                    else loosePosition = Vector3.up * (y + i * step);
                    outputVisuals.Add(RentItemVisual(
                        visibleOutputType, outputRoot, loosePosition, visualItemScale));
                }
            }
            UpdateLabel();
            // Every count change funnels through here, so this is the one place
            // the MAX flag can be kept honest. It used to be refreshed only when
            // a worker was hired, which left it lit on a station the player had
            // just emptied, and dark on one that had just filled up.
            UpdateMaxIndicator();
        }

        private static GameObject RentItemVisual(
            ItemType type, Transform parent, Vector3 localPosition, float scale)
        {
            GameObject visual = GameplayObjectPool.Rent(
                $"station.item.{type}.{scale:0.00}", parent,
                () => PrototypeVisuals.CreateItemVisual(type, parent, Vector3.zero, scale));
            if (visual == null) return null;

            visual.transform.localPosition = localPosition;
            PopIn.Play(visual, PrototypeVisuals.ItemSize(type) * scale);
            return visual;
        }

        private static void ClearVisuals(List<GameObject> visuals)
        {
            for (int i = 0; i < visuals.Count; i++)
                GameplayObjectPool.Release(visuals[i]);
            visuals.Clear();
        }

        private void UpdateLabel()
        {
            if (statusLabel == null) return;
            string countText = mode switch
            {
                StationMode.Source => outputCount.ToString(),
                StationMode.Service => outputCount.ToString(),
                _ => $"{inputCount}  →  {outputCount}"
            };
            statusLabel.text = $"{name}\n{countText}";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = PrototypeVisuals.Green;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
