using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class DailyTasksHUD : MonoBehaviour
    {
        private bool open;
        private GUIStyle titleStyle;
        private GUIStyle rowStyle;

        private void OnGUI()
        {
            EnsureStyles();
            if (GUI.Button(new Rect(12f, 174f, 52f, 52f), "✓")) open = !open;
            if (!open) return;

            Rect panel = new(72f, 150f, 270f, 214f);
            GUI.Box(panel, "");
            GUI.Label(new Rect(88f, 162f, 238f, 32f), "GUNLUK GOREVLER", titleStyle);
            DrawTask(202f, "5 musteri servis et", GameProgress.ServedToday, 5);
            DrawTask(242f, "1 gelistirme satin al", GameProgress.UpgradesToday, 1);
            DrawTask(282f, "3 cop at", GameProgress.TrashToday, 3);

            bool complete = GameProgress.ServedToday >= 5 && GameProgress.UpgradesToday >= 1 && GameProgress.TrashToday >= 3;
            GUI.enabled = complete && !GameProgress.DailyRewardClaimed;
            if (GUI.Button(new Rect(102f, 326f, 210f, 28f), GameProgress.DailyRewardClaimed ? "ALINDI" : "ODUL: 75"))
            {
                GameProgress.ClaimDailyReward();
                GameEconomy.Instance?.AddCoins(75);
            }
            GUI.enabled = true;
        }

        private void DrawTask(float y, string text, int current, int target)
        {
            string mark = current >= target ? "[X]" : "[ ]";
            GUI.Label(new Rect(90f, y, 230f, 30f), $"{mark} {text}  {Mathf.Min(current, target)}/{target}", rowStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold };
            rowStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 14 };
            titleStyle.normal.textColor = rowStyle.normal.textColor = new Color(0.24f, 0.12f, 0.08f);
        }
    }
}
