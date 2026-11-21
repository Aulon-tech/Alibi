// ProgressPanel.cs — Profili i lojtarit: progres per kartat, yje, niveli aktual
// Paneli hapet nga menu kryesore (butoni "Profili")
using Alibi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class ProgressPanel : MonoBehaviour
    {
        RectTransform root;
        System.Action onClose;

        public void Show(Transform canvas, System.Action close)
        {
            onClose = close;
            BuildOverlay(canvas);
        }

        void BuildOverlay(Transform canvas)
        {
            var go = new GameObject("ProgressOverlay");
            go.transform.SetParent(canvas, false);
            root = go.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            // Sfond i erret transparente
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.04f, 0.06f, 0.94f);

            // Click on background to close
            var bgBtn = go.AddComponent<Button>();
            bgBtn.transition = Selectable.Transition.None;
            bgBtn.onClick.AddListener(Close);

            // Paneli qendror
            var panel = UI.Panel(root, "Panel", new Color(0.08f, 0.09f, 0.12f, 0.98f), 20);
            panel.rectTransform.anchorMin = new Vector2(0.15f, 0.1f);
            panel.rectTransform.anchorMax = new Vector2(0.85f, 0.9f);
            panel.rectTransform.offsetMin = panel.rectTransform.offsetMax = Vector2.zero;
            var panelR = panel.rectTransform;

            // Kornize ari
            UI.Border(panelR, Theme.WithAlpha(Theme.Gold, 0.5f), 20);

            // ── Header ──
            var hdr = UI.LabelAt(panelR, "PROFILI I DETEKTIVIT", Theme.Serif, 42, Theme.Gold,
                0, 24, 0, 60);
            hdr.rectTransform.anchorMin = new Vector2(0.05f, 1);
            hdr.rectTransform.anchorMax = new Vector2(0.95f, 1);
            hdr.rectTransform.pivot = new Vector2(0.5f, 1);
            hdr.alignment = TextAnchor.UpperCenter;

            // Vije dekorative nen titull
            var line = UI.Panel(panelR, "GoldLine", Theme.WithAlpha(Theme.Gold, 0.4f), 0, false);
            line.rectTransform.anchorMin = new Vector2(0.3f, 1);
            line.rectTransform.anchorMax = new Vector2(0.7f, 1);
            line.rectTransform.pivot = new Vector2(0.5f, 1);
            line.rectTransform.anchoredPosition = new Vector2(0, -90);
            line.rectTransform.sizeDelta = new Vector2(0, 2);

            // ── Statistika te pergjithshme ──
            int totalCards = 5;
            int wonCount = 0;
            int totalStars = 0;
            int maxStars = totalCards * 3;
            for (int k = 1; k <= totalCards; k++)
            {
                if (AlibiFlow.CardWon(k)) wonCount++;
                totalStars += AlibiFlow.BestStars(k);
            }

            float statsY = -110;
            BuildStatBox(panelR, 0.12f, statsY, "Kartat e perfundura", wonCount + " / " + totalCards);
            BuildStatBox(panelR, 0.40f, statsY, "Yjet e mbledhura", totalStars + " / " + maxStars);
            BuildStatBox(panelR, 0.68f, statsY, "Niveli aktual", LevelName(AlibiFlow.NextCard()));

            // ── Kartat detaje ──
            var cardsHdr = UI.LabelAt(panelR, "PROGRESI PER KARTE", Theme.SansBold, 18, Theme.Text2,
                0, 0, 0, 24);
            cardsHdr.rectTransform.anchorMin = new Vector2(0.05f, 1);
            cardsHdr.rectTransform.anchorMax = new Vector2(0.95f, 1);
            cardsHdr.rectTransform.pivot = new Vector2(0.5f, 1);
            cardsHdr.rectTransform.anchoredPosition = new Vector2(0, statsY - 120);
            cardsHdr.alignment = TextAnchor.UpperCenter;

            float cardStartY = statsY - 160;
            float cardH = 80;
            float cardGap = 10;

            for (int k = 1; k <= totalCards; k++)
            {
                float yOff = cardStartY - (k - 1) * (cardH + cardGap);
                BuildCardRow(panelR, k, yOff, cardH);
            }

            // ── Butoni mbyll ──
            var closeBtn = UI.Button(panelR, "✕", BtnStyle.Ghost, Close, 24, 8);
            closeBtn.Rect.anchorMin = closeBtn.Rect.anchorMax = new Vector2(1, 1);
            closeBtn.Rect.pivot = new Vector2(1, 1);
            closeBtn.Rect.anchoredPosition = new Vector2(-16, -16);
            closeBtn.Rect.sizeDelta = new Vector2(44, 44);

            // ── Butoni per resetim (posht) ──
            var resetBtn = UI.Button(panelR, "Pastro progresin", BtnStyle.Danger, () =>
            {
                AlibiFlow.ResetProgress();
                Close();
            }, 14, 8);
            resetBtn.Rect.anchorMin = new Vector2(0.5f, 0);
            resetBtn.Rect.anchorMax = new Vector2(0.5f, 0);
            resetBtn.Rect.pivot = new Vector2(0.5f, 0);
            resetBtn.Rect.anchoredPosition = new Vector2(0, 24);
            resetBtn.Rect.sizeDelta = new Vector2(200, 38);
        }

        void BuildStatBox(RectTransform parent, float anchorX, float yOff, string label, string value)
        {
            var box = UI.Panel(parent, "stat_" + label, Theme.WithAlpha(Theme.Gold, 0.06f), 12, false);
            box.rectTransform.anchorMin = new Vector2(anchorX, 1);
            box.rectTransform.anchorMax = new Vector2(anchorX + 0.22f, 1);
            box.rectTransform.pivot = new Vector2(0, 1);
            box.rectTransform.anchoredPosition = new Vector2(0, yOff);
            box.rectTransform.sizeDelta = new Vector2(0, 90);

            UI.Border(box.transform, Theme.WithAlpha(Theme.Gold, 0.2f), 12);

            // Vlera e madhe
            var valTxt = UI.Label(box.transform, value, Theme.Serif, 32, Theme.GoldText, TextAnchor.MiddleCenter);
            valTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.4f);
            valTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            valTxt.rectTransform.offsetMin = valTxt.rectTransform.offsetMax = Vector2.zero;

            // Etiketa
            var labTxt = UI.Label(box.transform, label, Theme.Sans, 12, Theme.Muted, TextAnchor.MiddleCenter);
            labTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
            labTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.4f);
            labTxt.rectTransform.offsetMin = labTxt.rectTransform.offsetMax = Vector2.zero;
        }

        void BuildCardRow(RectTransform parent, int card, float yOff, float h)
        {
            bool won = AlibiFlow.CardWon(card);
            bool unlocked = AlibiFlow.CardUnlocked(card);
            int stars = AlibiFlow.BestStars(card);

            // Ngjyra bazuar ne status
            Color rowBg = won ? new Color(0.08f, 0.12f, 0.06f, 0.5f) :
                           unlocked ? new Color(0.1f, 0.1f, 0.14f, 0.5f) :
                           new Color(0.06f, 0.06f, 0.08f, 0.5f);

            var row = UI.Panel(parent, "row_" + card, rowBg, 10, false);
            row.rectTransform.anchorMin = new Vector2(0.06f, 1);
            row.rectTransform.anchorMax = new Vector2(0.94f, 1);
            row.rectTransform.pivot = new Vector2(0.5f, 1);
            row.rectTransform.anchoredPosition = new Vector2(0, yOff);
            row.rectTransform.sizeDelta = new Vector2(0, h);

            UI.Border(row.transform, Theme.WithAlpha(won ? Theme.Gold : Theme.Line2, 0.3f), 10);

            // Numri i kartes
            var numTxt = UI.Label(row.transform, card.ToString(), Theme.Serif, 36,
                won ? Theme.Gold : unlocked ? Theme.Text : Theme.Dim, TextAnchor.MiddleCenter);
            numTxt.rectTransform.anchorMin = new Vector2(0, 0);
            numTxt.rectTransform.anchorMax = new Vector2(0.08f, 1);
            numTxt.rectTransform.offsetMin = numTxt.rectTransform.offsetMax = Vector2.zero;

            // Emri i kartes
            string cardTitle = CardTitle(card);
            var titleTxt = UI.Label(row.transform, cardTitle, Theme.SansSemi, 18,
                unlocked ? Theme.Text : Theme.Dim, TextAnchor.MiddleLeft);
            titleTxt.rectTransform.anchorMin = new Vector2(0.10f, 0.5f);
            titleTxt.rectTransform.anchorMax = new Vector2(0.55f, 1f);
            titleTxt.rectTransform.offsetMin = titleTxt.rectTransform.offsetMax = Vector2.zero;

            // Pershkrimi i statusit
            string status = won ? "E perfunduar" : unlocked ? "E hapur" : "E bllokuar";
            Color statusCol = won ? Theme.Hex("#7AC36A") : unlocked ? Theme.Gold : Theme.Dim;
            var statusTxt = UI.Label(row.transform, status, Theme.Sans, 13, statusCol, TextAnchor.MiddleLeft);
            statusTxt.rectTransform.anchorMin = new Vector2(0.10f, 0f);
            statusTxt.rectTransform.anchorMax = new Vector2(0.55f, 0.5f);
            statusTxt.rectTransform.offsetMin = statusTxt.rectTransform.offsetMax = Vector2.zero;

            // Yjet (3 mundeshme)
            float starX = 0.60f;
            for (int s = 0; s < 3; s++)
            {
                var starGo = new GameObject("star_" + s, typeof(RectTransform), typeof(Image));
                starGo.transform.SetParent(row.rectTransform, false);
                var starR = starGo.GetComponent<RectTransform>();
                starR.anchorMin = starR.anchorMax = new Vector2(starX + s * 0.06f, 0.5f);
                starR.sizeDelta = new Vector2(28, 28);
                var starImg = starGo.GetComponent<Image>();
                starImg.sprite = Theme.Star;
                starImg.color = s < stars ? Theme.Gold : Theme.WithAlpha(Theme.Dim, 0.3f);
                starImg.raycastTarget = false;
            }

            // Ikone kyc per te bllokuarat
            if (!unlocked)
            {
                var lockTxt = UI.Label(row.transform, "🔒", Theme.Sans, 20, Theme.Dim, TextAnchor.MiddleCenter);
                lockTxt.rectTransform.anchorMin = new Vector2(0.88f, 0);
                lockTxt.rectTransform.anchorMax = new Vector2(0.98f, 1);
                lockTxt.rectTransform.offsetMin = lockTxt.rectTransform.offsetMax = Vector2.zero;
            }
            else if (won)
            {
                var checkTxt = UI.Label(row.transform, "✓", Theme.SansBold, 24,
                    Theme.Hex("#7AC36A"), TextAnchor.MiddleCenter);
                checkTxt.rectTransform.anchorMin = new Vector2(0.88f, 0);
                checkTxt.rectTransform.anchorMax = new Vector2(0.98f, 1);
                checkTxt.rectTransform.offsetMin = checkTxt.rectTransform.offsetMax = Vector2.zero;
            }
        }

        string CardTitle(int card)
        {
            var spec = LevelCards.Spec(card);
            return spec != null ? "Karta " + card + " — " + spec.Name : "Karta " + card;
        }

        string LevelName(int next)
        {
            if (next <= 1) return "Fillestar";
            if (next <= 2) return "Bazik";
            if (next <= 3) return "I avancuar";
            if (next <= 4) return "Ekspert";
            return "Master";
        }

        void Close()
        {
            if (root != null) Destroy(root.gameObject);
            if (onClose != null) onClose();
            Destroy(this);
        }
    }
}
