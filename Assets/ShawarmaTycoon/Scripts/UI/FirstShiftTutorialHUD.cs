using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>Cozy first-session framing: opening card, compact steps and a clear finish.</summary>
    public sealed class FirstShiftTutorialHUD : MonoBehaviour
    {
        private GameObject scrim;
        private GameObject introCard;
        private GameObject finishCard;
        private GameObject stepCard;
        private Text stepCount;
        private Text stepTitle;
        private Text stepBody;

        public static FirstShiftTutorialHUD Create(
            RectTransform parent, Action startShift, Action enterFreePlay)
        {
            RectTransform root = UIFactory.Node("First Shift Tutorial", parent);
            UIFactory.Stretch(root);
            FirstShiftTutorialHUD hud = root.gameObject.AddComponent<FirstShiftTutorialHUD>();
            hud.Build(root, startShift, enterFreePlay);
            return hud;
        }

        public void ShowIntro()
        {
            scrim.SetActive(true);
            introCard.SetActive(true);
            finishCard.SetActive(false);
            stepCard.SetActive(false);
        }

        public void ShowStep(int current, int total, string title, string body)
        {
            scrim.SetActive(false);
            introCard.SetActive(false);
            finishCard.SetActive(false);
            stepCard.SetActive(true);
            stepCount.text = $"İLK VARDİYA  •  {current}/{total}";
            stepTitle.text = title;
            stepBody.text = body;
        }

        public void ShowFinish()
        {
            scrim.SetActive(true);
            introCard.SetActive(false);
            finishCard.SetActive(true);
            stepCard.SetActive(false);
        }

        public void HideAll()
        {
            scrim.SetActive(false);
            introCard.SetActive(false);
            finishCard.SetActive(false);
            stepCard.SetActive(false);
        }

        private void Build(RectTransform root, Action startShift, Action enterFreePlay)
        {
            Image shade = UIFactory.Panel("Warm Scrim", root, UITheme.Scrim, rounded: false);
            UIFactory.Stretch(shade.rectTransform);
            shade.raycastTarget = true;
            scrim = shade.gameObject;

            introCard = BuildCard(root, "Opening Card", UITheme.CreamLight);
            AddBadge(introCard.transform, "YENİ DÜKKÂN  •  1. GÜN");
            AddTitle(introCard.transform, "MEAT & EAT", new Vector2(0f, 110f));
            AddSubtitle(introCard.transform,
                "İLK VARDİYA", "Küçük dükkânın hazır. İlk siparişi birlikte çıkaralım;\nsonrası tamamen senin.");
            AddRhythmChips(introCard.transform);
            AddButton(introCard.transform, "DÜKKÂNI AÇ", UITheme.Terracotta, startShift);

            finishCard = BuildCard(root, "Finish Card", UITheme.CreamLight);
            AddBadge(finishCard.transform, "İLK SİPARİŞ TAMAM");
            AddTitle(finishCard.transform, "HARİKA İŞ!", new Vector2(0f, 105f));
            AddSubtitle(finishCard.transform,
                "DÜKKÂN ARTIK SENİN", "Mutfağın akışını öğrendin. Müşterileri mutlu et,\nkazancını topla ve restoranı büyüt.");
            AddFinishStamp(finishCard.transform);
            AddButton(finishCard.transform, "SERBEST OYUNA GEÇ", UITheme.Teal, enterFreePlay);

            Image step = UIFactory.Panel("Tutorial Step", root, UITheme.CounterPaper);
            UIFactory.Anchor(step.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -24f), new Vector2(610f, 132f));
            UIFactory.AddCartoonFinish(step, 2f, 6f);
            stepCard = step.gameObject;

            Image marker = UIFactory.Panel("Step Marker", step.transform, UITheme.Mustard);
            UIFactory.Anchor(marker.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(-260f, 0f), new Vector2(62f, 86f));
            Text markerText = UIFactory.DisplayLabel("Marker Text", marker.transform, "!", 38, UITheme.Ink);
            UIFactory.Stretch(markerText.rectTransform);

            stepCount = UIFactory.Label("Step Count", step.transform, string.Empty, 14,
                UITheme.Terracotta, TextAnchor.MiddleLeft);
            UIFactory.Anchor(stepCount.rectTransform, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(82f, -10f), new Vector2(490f, 24f));

            stepTitle = UIFactory.DisplayLabel("Step Title", step.transform, string.Empty, 25,
                UITheme.Ink, TextAnchor.MiddleLeft);
            UIFactory.Anchor(stepTitle.rectTransform, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(82f, -34f), new Vector2(490f, 38f));

            stepBody = UIFactory.Label("Step Body", step.transform, string.Empty, 17,
                UITheme.InkSoft, TextAnchor.UpperLeft, FontStyle.Normal);
            UIFactory.Anchor(stepBody.rectTransform, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(82f, -72f), new Vector2(490f, 48f));

            HideAll();
        }

        private static GameObject BuildCard(RectTransform root, string name, Color color)
        {
            Image card = UIFactory.Panel(name, root, color);
            UIFactory.Anchor(card.rectTransform, UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(650f, 500f));
            UIFactory.AddCartoonFinish(card, 4f, 10f);

            Image stripe = UIFactory.Panel("Painted Stripe", card.transform, UITheme.Mustard);
            UIFactory.Anchor(stripe.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -16f), new Vector2(560f, 12f));
            stripe.rectTransform.localEulerAngles = new Vector3(0f, 0f, -1.5f);
            return card.gameObject;
        }

        private static void AddBadge(Transform card, string content)
        {
            Image badge = UIFactory.Panel("Badge", card, UITheme.DarkBlueGray);
            UIFactory.Anchor(badge.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -42f), new Vector2(300f, 42f));
            Text label = UIFactory.Label("Badge Text", badge.transform, content, 15, Color.white);
            UIFactory.Stretch(label.rectTransform, 10f, 3f);
        }

        private static void AddTitle(Transform card, string content, Vector2 offset)
        {
            Text title = UIFactory.DisplayLabel("Brand", card, content, 46, UITheme.Terracotta);
            UIFactory.Anchor(title.rectTransform, UIFactory.Center, UIFactory.Center,
                offset, new Vector2(580f, 64f));
        }

        private static void AddSubtitle(Transform card, string titleContent, string bodyContent)
        {
            Text title = UIFactory.DisplayLabel("Title", card, titleContent, 29, UITheme.Ink);
            UIFactory.Anchor(title.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 48f), new Vector2(570f, 44f));

            Text body = UIFactory.Label("Body", card, bodyContent, 19, UITheme.InkSoft,
                TextAnchor.MiddleCenter, FontStyle.Normal);
            body.lineSpacing = 1.15f;
            UIFactory.Anchor(body.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -8f), new Vector2(570f, 72f));
        }

        private static void AddRhythmChips(Transform card)
        {
            string[] captions = { "1  PİŞİR", "2  HAZIRLA", "3  SERVİS ET" };
            Color[] colors = { UITheme.Mustard, UITheme.Teal, UITheme.Terracotta };
            for (int i = 0; i < captions.Length; i++)
            {
                Image chip = UIFactory.Panel("Rhythm " + i, card, colors[i]);
                UIFactory.Anchor(chip.rectTransform, UIFactory.Center, UIFactory.Center,
                    new Vector2((i - 1) * 178f, -92f), new Vector2(158f, 48f));
                Text label = UIFactory.Label("Label", chip.transform, captions[i], 15,
                    i == 0 ? UITheme.Ink : Color.white);
                UIFactory.Stretch(label.rectTransform, 6f, 3f);
            }
        }

        private static void AddFinishStamp(Transform card)
        {
            Image stamp = UIFactory.Panel("Finish Stamp", card, UITheme.Mustard);
            UIFactory.Anchor(stamp.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -92f), new Vector2(250f, 52f));
            stamp.rectTransform.localEulerAngles = new Vector3(0f, 0f, -2f);
            Text label = UIFactory.DisplayLabel("Stamp Text", stamp.transform, "+ İLK KAZANÇ", 18, UITheme.Ink);
            UIFactory.Stretch(label.rectTransform, 8f, 3f);
        }

        private static void AddButton(Transform card, string caption, Color color, Action action)
        {
            Button button = UIFactory.Button("Primary Action", card, caption, color, Color.white, 20, action);
            UIFactory.Anchor(button.GetComponent<RectTransform>(), UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 34f), new Vector2(430f, 68f));
        }
    }
}
