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
        private int inputCount;
        private int outputCount;
        private float processTimer;
        private float sourceTimer;
        private float transferTimer;
        private bool workerAssigned;
        private int workerLevel;
        private GameObject workerVisual;
        private Vector3 workerVisualAnchor = new(0f, 0f, 1.25f);

        public StationMode Mode => mode;
        public ItemType InputType => inputType;
        public ItemType OutputType => outputType;
        public int InputCount => inputCount;
        public int OutputCount => outputCount;
        public float ProcessProgress => processDuration <= 0f ? 0f : Mathf.Clamp01(processTimer / processDuration);
        public bool WorkerAssigned => workerAssigned;
        public int WorkerLevel => workerLevel;
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

        public void SetVisualLayout(Vector3 inputLocalPosition, Vector3 outputLocalPosition, float maxLabelHeight)
        {
            if (inputRoot != null) inputRoot.localPosition = inputLocalPosition;
            if (outputRoot != null) outputRoot.localPosition = outputLocalPosition;
            if (maxLabel != null)
                maxLabel.transform.localPosition = new Vector3(0f, Mathf.Max(0.5f, maxLabelHeight), 0f);
        }

        public void SetWorkerVisualAnchor(Vector3 localPosition)
        {
            workerVisualAnchor = localPosition;
            if (workerVisual != null) workerVisual.transform.localPosition = workerVisualAnchor;
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
            maxLabel = PrototypeVisuals.CreateLabel("MAX", transform, new Vector3(0f, 2.05f, 0f), 0.15f);
            maxLabel.color = PrototypeVisuals.Red;
            maxLabel.gameObject.SetActive(false);

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

            // A fed station works on its own. It used to stop the moment the
            // player stepped away, so the whole game was standing still beside a
            // machine waiting for it - the work was never carrying the food, it
            // was babysitting. What a hired worker buys now is speed, not the
            // ability to run at all.
            float workSpeed = workerAssigned
                ? (1f + Mathf.Max(0, workerLevel - 1) * 0.45f) * HumanResourcesSystem.WorkerSpeedMultiplier
                : 1f;
            processTimer += Time.deltaTime * workSpeed;
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
                    AudioDirector.Play(GameSfx.Drop, 0.7f);
                    RefreshVisuals();
                    return true;
                }
                return false;
            }

            if (inventory.HeldType == inputType && inputCount < EffectiveInputCapacity && inventory.TryRemove(inputType))
            {
                inputCount++;
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

        public void AssignWorker()
        {
            SetWorkerLevel(Mathf.Max(1, workerLevel));
        }

        public void SetWorkerLevel(int level)
        {
            workerLevel = Mathf.Max(0, level);
            workerAssigned = workerLevel > 0;
            RefreshWorkerVisual();
            UpdateLabel();
            UpdateMaxIndicator();
        }

        /// <summary>
        /// A hired worker used to show up as nothing but an emoji in the station
        /// label, so a fully staffed kitchen looked exactly like an empty one.
        /// Now they stand at the station: behind the counter on the +Z side,
        /// turned to face the serving side the player works from.
        /// </summary>
        private void RefreshWorkerVisual()
        {
            if (workerAssigned == (workerVisual != null)) return;

            if (!workerAssigned)
            {
                // Unparent first: Destroy only takes effect at the end of the
                // frame, so a re-hire in the same frame would otherwise find the
                // outgoing figure still standing at the station.
                workerVisual.transform.SetParent(null, false);
                Destroy(workerVisual);
                workerVisual = null;
                return;
            }

            workerVisual = new GameObject("İşçi");
            workerVisual.transform.SetParent(transform, false);
            workerVisual.transform.localPosition = workerVisualAnchor;
            // Models look along +Z, so facing the serving side is a half turn.
            workerVisual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);


            if (MeshyVisuals.TryAttachAuthored(
                    workerVisual.transform, "55_worker_red_backcap",
                    Vector3.zero, Vector3.zero) != null)
                return;

            // No model: the label emoji stays the only cue rather than parking a
            // bare capsule behind every counter.
            Destroy(workerVisual);
            workerVisual = null;
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
                maxLabel.gameObject.SetActive(true);
                return;
            }

            if (!string.IsNullOrEmpty(emptyWarning))
            {
                maxLabel.text = emptyWarning;
                maxLabel.gameObject.SetActive(outputCount <= 0);
                return;
            }

            maxLabel.gameObject.SetActive(
                mode != StationMode.Source && outputCount >= EffectiveOutputCapacity);
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
                float step = PrototypeVisuals.StackStep(inputType, StackScale);
                for (int i = 0; i < shownInput; i++)
                    inputVisuals.Add(RentItemVisual(
                        inputType, inputRoot, Vector3.up * (i * step), StackScale));
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

                float step = PrototypeVisuals.StackStep(visibleOutputType, StackScale);
                for (int i = 0; i < loose; i++)
                    outputVisuals.Add(RentItemVisual(
                        visibleOutputType, outputRoot, Vector3.up * (y + i * step), StackScale));
            }
            UpdateLabel();
            // Every count change funnels through here, so this is the one place
            // the MAX flag can be kept honest. It used to be refreshed only when
            // a worker was hired, which left it lit on a station the player had
            // just emptied, and dark on one that had just filled up.
            UpdateMaxIndicator();
        }

        /// <summary>Counter portions are drawn a little under carried size.</summary>
        private const float StackScale = 0.85f;

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
            string workerText = workerAssigned ? "  👷" : "";
            statusLabel.text = $"{name}{workerText}\n{countText}";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = PrototypeVisuals.Green;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
