using UnityEngine;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>Daily objectives and lifetime records, opened from two side tabs.</summary>
    public sealed class TasksPanel : MonoBehaviour
    {
        private enum Mode { None, Tasks, Records }

        private const int ServeTarget = 5;
        private const int UpgradeTarget = 1;
        private const int TrashTarget = 3;
        private const int DailyReward = 75;

        private Mode mode = Mode.None;
        private RectTransform panel;
        private Text title;
        private Text[] rows;
        private Button claimButton;
        private Text claimLabel;

        public static TasksPanel Create(RectTransform parent)
        {
            RectTransform root = UIFactory.Node("Tasks", parent);
            UIFactory.Stretch(root);
            TasksPanel tasks = root.gameObject.AddComponent<TasksPanel>();

            Image card = UIFactory.Panel("Card", root, UITheme.CounterPaper);
            UIFactory.Anchor(card.rectTransform, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(150f, -150f), new Vector2(560f, 430f));
            UIFactory.AddCartoonFinish(card, 3f, 8f);
            card.rectTransform.localEulerAngles = new Vector3(0f, 0f, 0.6f);
            tasks.panel = card.rectTransform;

            Image header = UIFactory.Panel("Header", card.transform, UITheme.Green);
            UIFactory.Anchor(header.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -16f), new Vector2(506f, 58f));

            tasks.title = UIFactory.DisplayLabel("Title", header.transform, "", 28, UITheme.CreamLight);
            UIFactory.Anchor(tasks.title.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, 0f), new Vector2(480f, 58f));

            tasks.rows = new Text[6];
            for (int i = 0; i < tasks.rows.Length; i++)
            {
                tasks.rows[i] = UIFactory.Label($"Row{i}", card.transform, "",
                    23, UITheme.Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
                UIFactory.Anchor(tasks.rows[i].rectTransform, UIFactory.TopLeft, UIFactory.TopLeft,
                    new Vector2(34f, -88f - i * 44f), new Vector2(500f, 40f));
            }

            tasks.claimButton = UIFactory.Button("Claim", card.transform, "", UITheme.Green,
                UITheme.CreamLight, 23, tasks.Claim);
            UIFactory.Anchor(tasks.claimButton.GetComponent<RectTransform>(),
                UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 24f), new Vector2(350f, 64f));
            tasks.claimLabel = tasks.claimButton.GetComponentInChildren<Text>();

            tasks.CreateTab(root, "✓", "GÖREV", -142f, Mode.Tasks);
            tasks.CreateTab(root, "★", "REKOR", -242f, Mode.Records);

            card.gameObject.SetActive(false);
            return tasks;
        }

        private void CreateTab(
            RectTransform parent, string glyph, string caption, float y, Mode target)
        {
            Color background = target == Mode.Tasks ? UITheme.Green : UITheme.Teal;
            Button button = UIFactory.IconButton("Tab" + caption, parent, glyph, caption,
                background, UITheme.CreamLight, () => Toggle(target));
            UIFactory.Anchor(button.GetComponent<RectTransform>(), UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(24f, y), new Vector2(86f, 86f));
        }

        private void Toggle(Mode target)
        {
            mode = mode == target ? Mode.None : target;
            panel.gameObject.SetActive(mode != Mode.None);
            Refresh();
        }

        private void Claim()
        {
            if (!TasksComplete() || GameProgress.DailyRewardClaimed) return;
            GameProgress.ClaimDailyReward();
            if (GameEconomy.Instance != null) GameEconomy.Instance.AddCoins(DailyReward);
            AudioDirector.Play(GameSfx.Reward);
            Refresh();
        }

        private static bool TasksComplete() =>
            GameProgress.ServedToday >= ServeTarget &&
            GameProgress.UpgradesToday >= UpgradeTarget &&
            GameProgress.TrashToday >= TrashTarget;

        private void Update()
        {
            if (mode != Mode.None) Refresh();
        }

        private void Refresh()
        {
            if (mode == Mode.None) return;

            if (mode == Mode.Tasks)
            {
                title.text = "GÜNLÜK GÖREVLER";
                rows[0].text = Task("5 müşteri servis et", GameProgress.ServedToday, ServeTarget);
                rows[1].text = Task("1 geliştirme satın al", GameProgress.UpgradesToday, UpgradeTarget);
                rows[2].text = Task("3 çöp temizle", GameProgress.TrashToday, TrashTarget);
                rows[3].text = "";
                // Walk-outs are the only way to lose ground, so they get their own
                // line rather than being buried in the records tab.
                int lost = GameProgress.LostToday;
                rows[4].text = lost > 0 ? $"⚠  Bugün kaçan müşteri:   {lost}" : "";
                rows[4].color = UITheme.WarmRed;
                rows[5].text = "";

                bool claimable = TasksComplete() && !GameProgress.DailyRewardClaimed;
                claimButton.gameObject.SetActive(true);
                claimButton.interactable = claimable;
                claimLabel.text = GameProgress.DailyRewardClaimed
                    ? "ÖDÜL ALINDI"
                    : $"ÖDÜL  ₺{DailyReward}";
            }
            else
            {
                title.text = "RESTORAN REKORLARI";
                // The tasks view tints one row red; both views share the same rows.
                for (int i = 0; i < rows.Length; i++) rows[i].color = UITheme.Ink;
                rows[0].text = Record("Bugünkü gelir", "₺" + GameProgress.RevenueToday);
                rows[1].text = Record("En iyi gün", "₺" + GameProgress.BestDailyRevenue);
                rows[2].text = Record("Bugün servis", GameProgress.ServedToday.ToString());
                rows[3].text = Record("En çok servis", GameProgress.BestDailyServed.ToString());
                rows[4].text = Record("En iyi kombo", GameProgress.BestCombo.ToString());
                rows[5].text = Record("VIP / Paket",
                    GameProgress.VipServedTotal + " / " + GameProgress.TakeawayServedTotal);
                claimButton.gameObject.SetActive(false);
            }
        }

        private static string Task(string text, int current, int target) =>
            $"{(current >= target ? "✔" : "○")}  {text}    {Mathf.Min(current, target)}/{target}";

        private static string Record(string label, string value) => $"{label}:   {value}";
    }
}
