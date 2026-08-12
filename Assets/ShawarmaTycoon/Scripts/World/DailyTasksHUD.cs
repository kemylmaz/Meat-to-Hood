using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class DailyTasksHUD : MonoBehaviour
    {
        private enum PanelMode { None, Tasks, Records }

        private PanelMode mode;
        private GUIStyle titleStyle;
        private GUIStyle rowStyle;
        private GUIStyle smallButtonStyle;

        private void OnGUI()
        {
            EnsureStyles();
            Rect safe = Screen.safeArea.width > 0f ? Screen.safeArea : new Rect(0f, 0f, Screen.width, Screen.height);
            float left = safe.xMin + 12f;
            float top = Screen.height - safe.yMax;
            float buttonY = top + 174f;

            if (GUI.Button(new Rect(left, buttonY, 54f, 50f), "LIST", smallButtonStyle))
                mode = mode == PanelMode.Tasks ? PanelMode.None : PanelMode.Tasks;
            if (GUI.Button(new Rect(left, buttonY + 58f, 54f, 50f), "BEST", smallButtonStyle))
                mode = mode == PanelMode.Records ? PanelMode.None : PanelMode.Records;
            if (mode == PanelMode.None) return;

            float panelX = left + 62f;
            float panelY = buttonY - 24f;
            if (mode == PanelMode.Tasks)
                DrawTasks(new Rect(panelX, panelY, 282f, 214f));
            else
                DrawRecords(new Rect(panelX, panelY, 292f, 256f));
        }

        private void DrawTasks(Rect panel)
        {
            GUI.Box(panel, string.Empty);
            GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 32f), "GUNLUK GOREVLER", titleStyle);
            DrawTask(panel, 52f, "5 musteri servis et", GameProgress.ServedToday, 5);
            DrawTask(panel, 92f, "1 gelistirme satin al", GameProgress.UpgradesToday, 1);
            DrawTask(panel, 132f, "3 cop temizle", GameProgress.TrashToday, 3);

            bool complete = GameProgress.ServedToday >= 5 && GameProgress.UpgradesToday >= 1 && GameProgress.TrashToday >= 3;
            GUI.enabled = complete && !GameProgress.DailyRewardClaimed;
            if (GUI.Button(
                    new Rect(panel.x + 30f, panel.y + 174f, panel.width - 60f, 28f),
                    GameProgress.DailyRewardClaimed ? "ALINDI" : "ODUL: 75"))
            {
                GameProgress.ClaimDailyReward();
                GameEconomy.Instance?.AddCoins(75);
            }
            GUI.enabled = true;
        }

        private void DrawRecords(Rect panel)
        {
            GUI.Box(panel, string.Empty);
            GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 32f), "RESTORAN REKORLARI", titleStyle);
            DrawRecord(panel, 52f, "Bugun gelir", "$" + GameProgress.RevenueToday);
            DrawRecord(panel, 82f, "En iyi gun", "$" + GameProgress.BestDailyRevenue);
            DrawRecord(panel, 112f, "Bugun servis", GameProgress.ServedToday.ToString());
            DrawRecord(panel, 142f, "En cok servis", GameProgress.BestDailyServed.ToString());
            DrawRecord(panel, 172f, "En iyi kombo", GameProgress.BestCombo.ToString());
            DrawRecord(panel, 202f, "VIP / Paket", GameProgress.VipServedTotal + " / " + GameProgress.TakeawayServedTotal);
        }

        private void DrawTask(Rect panel, float yOffset, string text, int current, int target)
        {
            string mark = current >= target ? "[X]" : "[ ]";
            GUI.Label(
                new Rect(panel.x + 18f, panel.y + yOffset, panel.width - 36f, 30f),
                $"{mark} {text}  {Mathf.Min(current, target)}/{target}", rowStyle);
        }

        private void DrawRecord(Rect panel, float yOffset, string label, string value)
        {
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + yOffset, panel.width - 40f, 28f),
                label + ":  " + value, rowStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            rowStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14
            };
            smallButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = rowStyle.normal.textColor = new Color(0.24f, 0.12f, 0.08f);
        }
    }
}
