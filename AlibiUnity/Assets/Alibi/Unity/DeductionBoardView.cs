using System;
using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class DeductionBoardView : MonoBehaviour
    {
        public Action<int> OnHourClicked;
        public Action<int> OnSuspectClicked;
        public PropagationAnimator Animator;

        GameSession session;
        CaseDefinition c;
        Image[,,] chips;
        Text[,,] chipTexts;
        Image[,] cells;
        Text[] status;
        UIButton[] headers;
        Text candidatesText;
        RectTransform candidatesRow;
        VillaView villa;
        int currentTime;
        bool truthMode;

        public static float PreferredHeight(CaseDefinition c)
        {
            return 44 + 38 + c.N * 54 + 34;
        }

        public void Build(RectTransform parent, GameSession s, VillaView v)
        {
            session = s;
            c = s.Case;
            villa = v;
            var card = UI.Card(parent, "BoardCard", Theme.Panel, Theme.Line, 14);
            UI.Stretch(card.rectTransform, 0, 0, 0, 0);
            var root = card.transform;
            UI.LabelAt(root, "Tabela e deduksionit", Theme.Serif, 28, Theme.Parchment, 18, 8, 420, 36);
            candidatesRow = UI.Rect(root, "Candidates");
            UI.PlaceRight(candidatesRow, 16, 10, 420, 32);
            candidatesText = UI.Label(candidatesRow, "", Theme.Sans, 15, Theme.Text2, TextAnchor.MiddleRight);
            UI.Stretch(candidatesText.rectTransform, 0, 0, 0, 0);

            var grid = UI.Rect(root, "Grid");
            UI.Stretch(grid, 16, 48, 16, 34);
            var nameCol = UI.Rect(grid, "Names");
            nameCol.anchorMin = new Vector2(0, 0);
            nameCol.anchorMax = new Vector2(0, 1);
            nameCol.pivot = new Vector2(0, 1);
            nameCol.offsetMin = new Vector2(0, 0);
            nameCol.offsetMax = new Vector2(176, 0);
            UI.LabelAt(nameCol, "I DYSHUARI", Theme.Mono, 12, Theme.Muted, 4, 8, 170, 22);
            status = new Text[c.N];
            for (int i = 0; i < c.N; i++)
            {
                int si = i;
                var row = UI.Rect(nameCol, "Row" + i);
                UI.Place(row, 0, 38 + i * 54, 176, 50);
                UI.Avatar(row, c.Suspects[i], 34, 2, 8);
                UI.LabelAt(row, c.Suspects[i].Name, Theme.SansSemi, 16, Theme.Text, 44, 6, 130, 22);
                status[i] = UI.LabelAt(row, "", Theme.Sans, 12, Theme.Gold, 44, 27, 130, 18);
                var hit = row.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
                row.gameObject.AddComponent<ClickRelay>().OnClick = () => { if (OnSuspectClicked != null) OnSuspectClicked(si); };
            }

            var hours = UI.Rect(grid, "Hours");
            UI.Stretch(hours, 182, 0, 0, 0);
            chips = new Image[c.N, c.T, c.RoomCount];
            chipTexts = new Text[c.N, c.T, c.RoomCount];
            cells = new Image[c.N, c.T];
            headers = new UIButton[c.T];
            for (int t = 0; t < c.T; t++)
            {
                int tt = t;
                var col = UI.Rect(hours, "Hour" + t);
                col.anchorMin = new Vector2(t / (float)c.T, 0);
                col.anchorMax = new Vector2((t + 1) / (float)c.T, 1);
                col.offsetMin = new Vector2(3, 0);
                col.offsetMax = new Vector2(-3, 0);
                string sub = c.CrimeTimeKnown ? (t == c.CrimeTime ? "   <size=11><color=#F09A8F>KRIMI</color></size>" : "") : "   <size=11><color=#F09A8F>?</color></size>";
                var h = UI.Button(col, c.Hours[t] + sub, BtnStyle.Chip, () => { if (OnHourClicked != null) OnHourClicked(tt); }, 15, 8);
                h.Rect.anchorMin = new Vector2(0, 1);
                h.Rect.anchorMax = new Vector2(1, 1);
                h.Rect.pivot = new Vector2(0.5f, 1);
                h.Rect.offsetMin = new Vector2(0, -32);
                h.Rect.offsetMax = new Vector2(0, 0);
                headers[t] = h;
                for (int s2 = 0; s2 < c.N; s2++)
                {
                    var cell = UI.Panel(col, "Cell", new Color(1, 1, 1, 0.015f), 8, false);
                    var crt = cell.rectTransform;
                    crt.anchorMin = new Vector2(0, 1);
                    crt.anchorMax = new Vector2(1, 1);
                    crt.pivot = new Vector2(0.5f, 1);
                    crt.offsetMin = new Vector2(0, -(38 + s2 * 54 + 50));
                    crt.offsetMax = new Vector2(0, -(38 + s2 * 54));
                    cells[s2, t] = cell;
                    var gridLayout = cell.gameObject.AddComponent<GridLayoutGroup>();
                    gridLayout.cellSize = new Vector2(40, 20);
                    gridLayout.spacing = new Vector2(4, 4);
                    gridLayout.padding = new RectOffset(5, 5, 3, 3);
                    gridLayout.childAlignment = TextAnchor.MiddleLeft;
                    gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
                    for (int r = 0; r < c.RoomCount; r++)
                    {
                        var chip = UI.Panel(cell.transform, "Chip", Theme.Line, 5, false);
                        var txt = UI.Label(chip.transform, c.Rooms[r].Id, Theme.Mono, 12, Theme.Text, TextAnchor.MiddleCenter);
                        UI.Stretch(txt.rectTransform, 0, 0, 0, 0);
                        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                        chips[s2, t, r] = chip;
                        chipTexts[s2, t, r] = txt;
                    }
                }
            }

            var legend = UI.Rect(root, "Legend");
            UI.StretchBottom(legend, 18, 6, 18, 24);
            var parts = new List<string>();
            foreach (var r in c.Rooms) parts.Add("<color=#ECE6D8>" + r.Id + "</color> " + r.Name);
            var lt = UI.Label(legend, string.Join("   ", parts.ToArray()) + "      <color=#D2AD62>ari</color> = e konfirmuar · <color=#C9C2B2>kuti</color> = e mundshme · <color=#6E747D>zbehur</color> = e përjashtuar", Theme.Sans, 13, Theme.Muted, TextAnchor.MiddleLeft);
            UI.Stretch(lt.rectTransform, 0, 0, 0, 0);
            lt.horizontalOverflow = HorizontalWrapMode.Overflow;

            s.Domains.OnDomainChanged += HandleDomainChanged;
        }

        void OnDestroy()
        {
            if (session != null) session.Domains.OnDomainChanged -= HandleDomainChanged;
        }

        void HandleDomainChanged(List<CellChange> changes)
        {
            Refresh(currentTime, truthMode);
            if (Animator == null || chips == null) return;
            foreach (var ch in changes)
            {
                Animator.Flash(chips[ch.Suspect, ch.Time, ch.Room], new Color(0.9f, 0.35f, 0.3f, 1f), 1.2f);
            }
        }

        public void Rebind(GameSession s)
        {
            if (session != null) session.Domains.OnDomainChanged -= HandleDomainChanged;
            session = s;
            session.Domains.OnDomainChanged += HandleDomainChanged;
        }

        public void Refresh(int time, bool truth)
        {
            currentTime = time;
            truthMode = truth;
            var dm = session.Domains;
            int candidates = 0;
            var names = new List<string>();
            for (int s = 0; s < c.N; s++)
            {
                bool cand = dm.Candidate[s];
                if (cand)
                {
                    candidates++;
                    names.Add("<color=" + c.Suspects[s].ColorHex + ">" + c.Suspects[s].Name + "</color>");
                }
                if (truth)
                {
                    status[s].text = s == c.Culprit ? "fajtori" : "i ndershëm";
                    status[s].color = s == c.Culprit ? Theme.RedText : Theme.Blue;
                }
                else
                {
                    status[s].text = cand ? "kandidat" : "jashtë dyshimit";
                    status[s].color = cand ? Theme.Gold : Theme.Blue;
                }
                for (int t = 0; t < c.T; t++)
                {
                    cells[s, t].color = t == time ? Theme.WithAlpha(Theme.Gold, 0.07f) : new Color(1, 1, 1, 0.015f);
                    for (int r = 0; r < c.RoomCount; r++)
                    {
                        var chip = chips[s, t, r];
                        var txt = chipTexts[s, t, r];
                        bool possible = dm.Possible[s, t, r];
                        bool confirmed = possible && dm.Count[s, t] == 1;
                        bool crime = r == c.CrimeRoom && (!c.CrimeTimeKnown || t == c.CrimeTime);
                        if (truth)
                        {
                            bool real = c.Truth[s][t] == r;
                            chip.color = real ? (s == c.Culprit && r == c.CrimeRoom && t == c.CrimeTime ? Theme.Red : Theme.Gold) : new Color(1, 1, 1, 0.02f);
                            txt.color = real ? Theme.Ink : Theme.WithAlpha(Theme.Dim, 0.6f);
                        }
                        else if (confirmed)
                        {
                            chip.color = Theme.Gold;
                            txt.color = Theme.Ink;
                        }
                        else if (possible)
                        {
                            chip.color = crime ? Theme.RedSoft : Theme.Panel3;
                            txt.color = crime ? Theme.RedText : Theme.Text;
                        }
                        else
                        {
                            chip.color = new Color(1, 1, 1, 0.02f);
                            txt.color = Theme.WithAlpha(Theme.Dim, 0.55f);
                        }
                    }
                }
            }
            for (int t = 0; t < c.T; t++) headers[t].SetStyle(t == time ? BtnStyle.ChipOn : BtnStyle.Chip);
            candidatesText.text = truth ? "Rasti u mbyll" : "Kandidatë: " + candidates + " nga " + c.N + "   " + string.Join(" · ", names.ToArray());
            if (villa != null) UpdateMarkers(time, truth);
        }

        public void UpdateMarkers(int time, bool truth)
        {
            var dm = session.Domains;
            var conflictSuspects = new HashSet<int>();
            if (!truth && session.Conflicts.Count > 0)
            {
                var last = session.Conflicts[session.Conflicts.Count - 1];
                if (last.At == session.Statements.Count)
                {
                    foreach (var id in last.StatementIds) conflictSuspects.Add(session.Statements[id - 1].Suspect);
                }
            }
            var steps = new List<Action>();
            for (int s = 0; s < c.N; s++)
            {
                for (int r = 0; r < c.RoomCount; r++)
                {
                    MarkerState st;
                    if (truth) st = c.Truth[s][time] == r ? MarkerState.Confirmed : MarkerState.Hidden;
                    else if (!dm.Possible[s, time, r]) st = MarkerState.Hidden;
                    else st = dm.Count[s, time] == 1 ? MarkerState.Confirmed : MarkerState.Possible;
                    var marker = villa.Markers[s, r];
                    bool conflict = conflictSuspects.Contains(s);
                    if (marker.State == st && marker.Conflict == conflict) continue;
                    var m = marker;
                    var state = st;
                    steps.Add(() => m.SetState(state, conflict));
                }
            }
            if (Animator != null && steps.Count > 0) Animator.Play(steps, 0.035f);
            else foreach (var step in steps) step();
            villa.SetCrimeActive(c.CrimeTimeKnown ? time == c.CrimeTime : truth && time == c.CrimeTime);
            if (villa.CrimeDecal != null) villa.CrimeDecal.SetActive(true);
        }
    }
}
