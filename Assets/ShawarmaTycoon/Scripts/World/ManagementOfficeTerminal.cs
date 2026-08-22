using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Two desks, two menus. Recruiting used to have a room and a desk of its own;
    /// hiring is what a human resources office does, so it moved onto that desk
    /// and the third door went away.
    /// </summary>
    public enum ManagementMenu { HumanResources, GeneralManager }

    /// <summary>Opens the appropriate management window once when the player reaches an office desk.</summary>
    public sealed class ManagementOfficeTerminal : MonoBehaviour
    {
        [SerializeField, Min(0.4f)] private float activationRadius = 1.15f;
        private Transform player;
        private ManagementMenuHUD menu;
        private ManagementMenu type;
        private TextMesh label;
        private bool wasNearby;

        public void Configure(Transform playerTransform, ManagementMenuHUD menuHud, ManagementMenu menuType, string displayName)
        {
            player = playerTransform;
            menu = menuHud;
            type = menuType;
            float badgeWidth = Mathf.Clamp(0.75f + displayName.Length * 0.075f, 1.15f, 2.10f);
            label = PrototypeVisuals.CreateCozyBadge(
                displayName, transform, Vector3.up * 1.58f, badgeWidth,
                UI.UITheme.CreamLight, UI.UITheme.Ink);
            label.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (player == null || menu == null) return;
            bool nearby = Vector3.SqrMagnitude(player.position - transform.position) <= activationRadius * activationRadius;
            if (label != null) label.gameObject.SetActive(nearby);
            if (nearby && !wasNearby) menu.Open(type);
            wasNearby = nearby;
        }

        private void OnDisable()
        {
            if (menu != null) menu.Close(type);
        }
    }
}
