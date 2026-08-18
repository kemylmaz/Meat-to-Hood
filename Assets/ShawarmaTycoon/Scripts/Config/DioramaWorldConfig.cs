using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    [CreateAssetMenu(
        menuName = "Shawarma Tycoon/Configuration/Diorama World",
        fileName = "DioramaWorldConfig")]
    public sealed class DioramaWorldConfig : ScriptableObject
    {
        [Serializable]
        public struct ExpansionDefinition
        {
            [SerializeField] private string id;
            [SerializeField] private Vector3 position;
            [SerializeField] private Vector2 size;

            public string Id => id;
            public Vector3 Position => position;
            public Vector2 Size => size;

            public ExpansionDefinition(string moduleId, Vector3 modulePosition, Vector2 moduleSize)
            {
                id = moduleId;
                position = modulePosition;
                size = moduleSize;
            }
        }

        public const float AuthoredPlotSpan = 5.64f;

        [Header("Core Island")]
        [SerializeField] private string coreId = "core";
        [SerializeField] private Vector3 corePosition;
        [SerializeField] private Vector2 coreSize = new(AuthoredPlotSpan * 4f, AuthoredPlotSpan * 3f);
        [SerializeField, Min(0.1f)] private float deckThickness = 0.40f;
        [SerializeField, Min(1f)] private float undersideDepth = 3.2f;
        [SerializeField, Min(1f)] private float floorTileSpan = AuthoredPlotSpan;

        [Header("Expansion Modules")]
        [SerializeField] private ExpansionDefinition[] expansions =
        {
            new("dining-wing", new Vector3(AuthoredPlotSpan * 2.5f, 0f, -AuthoredPlotSpan * 0.5f),
                new Vector2(AuthoredPlotSpan, AuthoredPlotSpan)),
            new("service-wing", new Vector3(AuthoredPlotSpan * 2.5f, 0f, AuthoredPlotSpan * 0.5f),
                new Vector2(AuthoredPlotSpan, AuthoredPlotSpan))
        };

        [Header("Player and Camera")]
        [SerializeField] private Vector3 playerSpawn = new(-1.5f, 0.26f, 0f);
        [SerializeField, Min(0.05f)] private float edgeSafetyMargin = 0.28f;

        [Header("Visual Fit")]
        [SerializeField] private float floorVisualSink = -0.23f;
        [SerializeField] private float expansionVisualDeckOffset = -1.36f;

        public string CoreId => coreId;
        public Vector3 CorePosition => corePosition;
        public Vector2 CoreSize => coreSize;
        public float DeckThickness => Mathf.Max(0.1f, deckThickness);
        public float DeckTopY => corePosition.y + DeckThickness * 0.5f;
        public float UndersideDepth => Mathf.Max(1f, undersideDepth);
        public float FloorTileSpan => Mathf.Max(1f, floorTileSpan);
        public IReadOnlyList<ExpansionDefinition> Expansions => expansions;
        public Vector3 PlayerSpawn => playerSpawn;
        public float EdgeSafetyMargin => Mathf.Max(0.05f, edgeSafetyMargin);
        public float FloorVisualSink => floorVisualSink;
        public float ExpansionVisualDeckOffset => expansionVisualDeckOffset;

        public static DioramaWorldConfig CreateRuntimeDefaults()
        {
            DioramaWorldConfig config = CreateInstance<DioramaWorldConfig>();
            config.name = "Runtime Diorama World";
            return config;
        }

#if UNITY_EDITOR
        public void RestorePhase3Defaults()
        {
            coreId = "core";
            corePosition = Vector3.zero;
            coreSize = new Vector2(AuthoredPlotSpan * 4f, AuthoredPlotSpan * 3f);
            deckThickness = 0.40f;
            undersideDepth = 3.2f;
            floorTileSpan = AuthoredPlotSpan;
            expansions = new[]
            {
                new ExpansionDefinition("dining-wing",
                    new Vector3(AuthoredPlotSpan * 2.5f, 0f, -AuthoredPlotSpan * 0.5f),
                    new Vector2(AuthoredPlotSpan, AuthoredPlotSpan)),
                new ExpansionDefinition("service-wing",
                    new Vector3(AuthoredPlotSpan * 2.5f, 0f, AuthoredPlotSpan * 0.5f),
                    new Vector2(AuthoredPlotSpan, AuthoredPlotSpan))
            };
            playerSpawn = new Vector3(-1.5f, 0.26f, 0f);
            edgeSafetyMargin = 0.28f;
            floorVisualSink = -0.23f;
            expansionVisualDeckOffset = -1.36f;
        }
#endif
    }
}
