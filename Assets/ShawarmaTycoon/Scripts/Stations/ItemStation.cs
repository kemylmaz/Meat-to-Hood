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

            if (mode == StationMode.Source)
                outputCount = outputCapacity;

            RefreshVisuals();
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
            if (outputCount >= outputCapacity) return;
            sourceTimer += Time.deltaTime;
            if (sourceTimer < sourceInterval) return;
            sourceTimer = 0f;
            outputCount++;
            RefreshVisuals();
        }

        private void UpdateProcessor()
        {
            if (inputCount <= 0 || outputCount >= outputCapacity)
            {
                processTimer = 0f;
                return;
            }

            processTimer += Time.deltaTime;
            if (processTimer < processDuration) return;

            processTimer = 0f;
            inputCount--;
            outputCount++;
            RefreshVisuals();
        }

        private bool TryInteract()
        {
            if (mode == StationMode.Source)
                return TryGiveOutputToPlayer();

            if (mode == StationMode.Service)
            {
                if (inventory.HeldType == inputType && outputCount < outputCapacity && inventory.TryRemove(inputType))
                {
                    outputCount++;
                    RefreshVisuals();
                    return true;
                }
                return false;
            }

            if (inventory.HeldType == inputType && inputCount < inputCapacity && inventory.TryRemove(inputType))
            {
                inputCount++;
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
            RefreshVisuals();
            return true;
        }

        public bool TryTakeServiceItem()
        {
            if (mode != StationMode.Service || outputCount <= 0) return false;
            outputCount--;
            RefreshVisuals();
            return true;
        }

        private void RefreshVisuals()
        {
            ClearVisuals(inputVisuals);
            ClearVisuals(outputVisuals);

            int shownInput = Mathf.Min(inputCount, 6);
            int shownOutput = Mathf.Min(outputCount, 8);

            if (inputType != ItemType.None && inputRoot != null)
            {
                for (int i = 0; i < shownInput; i++)
                    inputVisuals.Add(PrototypeVisuals.CreateItemVisual(inputType, inputRoot, Vector3.up * (i * 0.12f), 0.85f));
            }

            ItemType visibleOutputType = mode == StationMode.Service ? inputType : outputType;
            if (visibleOutputType != ItemType.None && outputRoot != null)
            {
                for (int i = 0; i < shownOutput; i++)
                    outputVisuals.Add(PrototypeVisuals.CreateItemVisual(visibleOutputType, outputRoot, Vector3.up * (i * 0.12f), 0.85f));
            }
            UpdateLabel();
        }

        private static void ClearVisuals(List<GameObject> visuals)
        {
            for (int i = 0; i < visuals.Count; i++)
                if (visuals[i] != null) Destroy(visuals[i]);
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
