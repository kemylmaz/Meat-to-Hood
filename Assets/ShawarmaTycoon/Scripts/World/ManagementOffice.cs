using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// One office. The room is built with the shop and stands there empty; the
    /// desk and the person behind it arrive when the money pad inside has been
    /// paid off.
    ///
    /// The rooms used to be hidden whole until an unlock pad outside was paid,
    /// so the management wing was a blank strip of floor with three pads on it
    /// and nothing to say what any of them would put there.
    /// </summary>
    public sealed class ManagementOffice : MonoBehaviour
    {
        private Transform player;
        private ManagementMenuHUD menu;
        private ManagementMenu menuType;
        private string deskAsset;
        private string displayName;
        private Vector3 deskLocalPosition;
        private float deskYaw;
        private Transform furnishRoot;
        private GameObject emptyMarker;

        public bool IsFurnished { get; private set; }

        /// <summary>
        /// <paramref name="yaw"/> is which way the desk looks. The authored desks
        /// face +Z; turning one to face +X puts its front toward the camera, which
        /// is also the side the player walks up to it from.
        /// </summary>
        public void Configure(
            Transform playerTransform,
            ManagementMenuHUD menuHud,
            ManagementMenu type,
            string deskAssetId,
            string title,
            Vector3 deskPosition,
            float yaw)
        {
            player = playerTransform;
            menu = menuHud;
            menuType = type;
            deskAsset = deskAssetId;
            displayName = title;
            deskLocalPosition = deskPosition;
            deskYaw = yaw;

            GameObject root = new("Mobilya");
            root.transform.SetParent(transform, false);
            furnishRoot = root.transform;

            BuildEmptyMarker();
        }

        /// <summary>
        /// A taped-out rectangle where the desk will stand. Without it a paid-for
        /// room and an unpaid one look identical from the doorway.
        /// </summary>
        private void BuildEmptyMarker()
        {
            emptyMarker = new GameObject("Boş Oda İşareti");
            emptyMarker.transform.SetParent(transform, false);
            emptyMarker.transform.localPosition = deskLocalPosition + Vector3.up * 0.02f;
            emptyMarker.transform.localEulerAngles = new Vector3(0f, deskYaw, 0f);

            Color tape = new(0.86f, 0.72f, 0.30f);
            const float halfX = 1.32f;
            const float halfZ = 0.92f;
            PrototypeVisuals.CreatePrimitive("Kenar Kuzey", PrimitiveType.Cube, emptyMarker.transform,
                new Vector3(0f, 0f, halfZ), new Vector3(halfX * 2f, 0.02f, 0.08f), tape);
            PrototypeVisuals.CreatePrimitive("Kenar Güney", PrimitiveType.Cube, emptyMarker.transform,
                new Vector3(0f, 0f, -halfZ), new Vector3(halfX * 2f, 0.02f, 0.08f), tape);
            PrototypeVisuals.CreatePrimitive("Kenar Batı", PrimitiveType.Cube, emptyMarker.transform,
                new Vector3(-halfX, 0f, 0f), new Vector3(0.08f, 0.02f, halfZ * 2f), tape);
            PrototypeVisuals.CreatePrimitive("Kenar Doğu", PrimitiveType.Cube, emptyMarker.transform,
                new Vector3(halfX, 0f, 0f), new Vector3(0.08f, 0.02f, halfZ * 2f), tape);
        }

        /// <summary>
        /// Puts the desk, its terminal and the employee into the room. Called once
        /// when the pad is paid and again on load for a room that was already
        /// bought, which is why nothing here plays a purchase effect.
        /// </summary>
        public void Furnish()
        {
            if (IsFurnished) return;
            IsFurnished = true;
            if (emptyMarker != null) emptyMarker.SetActive(false);

            GameObject desk = new("Masa");
            desk.transform.SetParent(furnishRoot, false);
            desk.transform.localPosition = deskLocalPosition;
            desk.transform.localEulerAngles = new Vector3(0f, deskYaw, 0f);
            PlaceableObject placeable = desk.AddComponent<PlaceableObject>();
            placeable.Configure("office." + menuType.ToString().ToLowerInvariant(), displayName + " Masası");

            if (MeshyVisuals.TryAttachAuthored(
                    desk.transform, deskAsset, Vector3.zero, Vector3.zero) == null)
            {
                PrototypeVisuals.CreatePrimitive("Masa Gövdesi", PrimitiveType.Cube,
                    desk.transform, new Vector3(0f, 0.45f, 0.2f),
                    new Vector3(1.7f, 0.85f, 0.7f), PrototypeVisuals.Cream);
            }

            GameObject collisionProxy = new("Masa Çarpışma Kutusu");
            collisionProxy.transform.SetParent(desk.transform, false);
            collisionProxy.transform.localPosition = new Vector3(0f, 0.58f, 0.15f);
            collisionProxy.AddComponent<BoxCollider>().size = new Vector3(1.72f, 1.16f, 0.86f);

            CreateClerk(desk.transform);

            // The trigger sits in front of the desk, on the side its face is
            // turned to. Reading it off the model's own anchor put it behind the
            // desk whenever the desk was not left at its authored rotation.
            GameObject terminal = new("Terminal");
            terminal.transform.SetParent(desk.transform, false);
            terminal.transform.localPosition = new Vector3(0f, 0f, 1.35f);
            terminal.AddComponent<ManagementOfficeTerminal>()
                .Configure(player, menu, menuType, displayName);
        }

        /// <summary>The clerk stands behind the desk, facing the same way it does.</summary>
        private static void CreateClerk(Transform desk)
        {
            GameObject clerk = new("Görevli");
            clerk.transform.SetParent(desk, false);
            clerk.transform.localPosition = new Vector3(0f, 0f, -0.95f);
            if (MeshyVisuals.TryAttachAuthored(
                    clerk.transform, "03_cashier_worker", Vector3.zero, Vector3.zero) == null)
            {
                PrototypeVisuals.CreatePrimitive("Görevli Gövdesi", PrimitiveType.Capsule,
                    clerk.transform, new Vector3(0f, 0.72f, 0f),
                    new Vector3(0.42f, 0.72f, 0.42f), new Color(0.38f, 0.70f, 0.64f));
            }
        }
    }
}
