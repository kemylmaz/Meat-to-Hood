#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace ShawarmaTycoon.Tests
{
    public sealed class BuildModePersistenceTests
    {
        private MemorySaveProvider provider;

        [SetUp]
        public void SetUp()
        {
            SaveRepository.ResetStateForTests();
            provider = new MemorySaveProvider();
            SaveRepository.InitializeForTests(provider);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (PlaceableObject placeable in Object.FindObjectsByType<PlaceableObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (placeable != null && placeable.name.StartsWith("BuildModeTest"))
                    Object.DestroyImmediate(placeable.gameObject);
            GameObject parent = GameObject.Find("BuildModeTestParent");
            if (parent != null) Object.DestroyImmediate(parent);
            SaveRepository.ResetStateForTests();
        }

        [Test]
        public void PlaceableObject_RestoresLocalPositionAndQuarterTurnFromSave()
        {
            GameObject parent = new("BuildModeTestParent");
            parent.transform.position = new Vector3(5f, 0f, 7f);

            PlaceableObject first = CreatePlaceable(parent.transform);
            first.MoveWorld(new Vector3(8f, 0.25f, 11f));
            first.RotateQuarterTurn();
            first.Commit();
            Object.DestroyImmediate(first.gameObject);

            // Reinitialising the repository simulates a fresh application session.
            SaveRepository.ResetStateForTests();
            SaveRepository.InitializeForTests(provider);
            PlaceableObject restored = CreatePlaceable(parent.transform);

            Assert.That(restored.transform.localPosition.x, Is.EqualTo(3f).Within(0.001f));
            Assert.That(restored.transform.localPosition.z, Is.EqualTo(4f).Within(0.001f));
            Assert.That(restored.transform.localEulerAngles.y, Is.EqualTo(90f).Within(0.1f));
        }

        [Test]
        public void ResetToDefault_ReturnsAuthoredTransformAndPersistsIt()
        {
            GameObject parent = new("BuildModeTestParent");
            PlaceableObject placeable = CreatePlaceable(parent.transform);
            placeable.MoveWorld(new Vector3(4f, 0.25f, 5f));
            placeable.RotateQuarterTurn();
            placeable.Commit();

            placeable.ResetToDefault();
            placeable.Commit();

            Assert.That(placeable.transform.localPosition, Is.EqualTo(new Vector3(1f, 0.25f, 2f)));
            Assert.That(placeable.transform.localRotation, Is.EqualTo(Quaternion.identity));
        }

        private static PlaceableObject CreatePlaceable(Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "BuildModeTestProp";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(1f, 0.25f, 2f);
            PlaceableObject placeable = go.AddComponent<PlaceableObject>();
            placeable.Configure("test.prop", "Test Prop");
            placeable.EnsureInitialized();
            return placeable;
        }

        private sealed class MemorySaveProvider : ISaveProvider
        {
            private SaveData data;
            public bool TryLoad(out SaveData loaded) { loaded = data; return loaded != null; }
            public void Save(SaveData value) => data = value;
            public void Delete() => data = null;
        }
    }
}
#endif
