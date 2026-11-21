using Alibi.Core;
using UnityEngine;

namespace Alibi.Unity
{
    // Rrëfimi i kartave: hyrja (pamje me tekst, si IntroVideo) dhe mbyllja pas zgjidhjes.
    // Hyrja nuk zbulon fajtorin; mbyllja shfaqet vetëm kur rasti zgjidhet.
    // Faktet (personat, dhomat, orët) përputhen me LevelCards.Card3() dhe Card4().
    public static class CaseStory
    {
        public sealed class Story
        {
            public string Heading;
            public string[][] Slides; // { ikona, titulli, teksti }
            public string Epilogue;
        }

        static readonly Story Karta3 = new Story
        {
            Heading = "KARTA 3 · DORËSHKRIMI I HUMBUR",
            Slides = new[]
            {
                new[] { "✦", "Dorëshkrimi i vjetër", "Në Bibliotekën e vilës ruhej një dorëshkrim i rrallë, nën xhamin e një vitrine. Çelësin e kishte vetëm kujdestari." },
                new[] { "◉", "Ora 22:00", "Në dhjetë të mbrëmjes, kujdestari u gjet i vdekur pranë vitrinës. Xhami ishte hapur dhe dorëshkrimi ishte zhdukur." },
                new[] { "?", "Pesë njerëz, dy kate", "Arta, Blerimi, Drita, Elmazi dhe Fatmiri ishin në vilë. Shkallët të çojnë lart te Korridori. Askush nuk kalon nga një dhomë në tjetrën pa kaluar nëpër derë." },
                new[] { "!", "Një dëshmi do të gabojë", "Sonte, një dëshmitar i ndershëm do të ngatërrojë një përgjigje pa dashje. Një kontradiktë e vetme nuk mjafton: kërko dy." },
                new[] { "★", "Kush ishte në Bibliotekë?", "Fajtori ishte në Bibliotekë në 22:00. Ec nëpër vilë, pyet secilin dhe provoje me jo më shumë se 11 pyetje." }
            },
            Epilogue = "Elmazi, kopshtari, tha se kishte qenë në Sallon gjithë mbrëmjen. Por në 22:00 ishte në Bibliotekë, pranë vitrinës. "
                     + "Dorëshkrimi u gjet më vonë, i mbështjellë mes thasëve të kopshtit. Drita nuk gënjeu: thjesht ngatërroi orën kur tha se ishte në Kuzhinë, ku shkoi vetëm më vonë."
        };

        static readonly Story Karta4 = new Story
        {
            Heading = "KARTA 4 · KALIMI I FSHEHTË",
            Slides = new[]
            {
                new[] { "✦", "Dosja e kontratave", "Në Zyrën e katit të parë, pronari mbante në kasafortë dosjen me kontratat e vilës. Nesër do t'i nënshkruante te noteri." },
                new[] { "?", "Kasaforta e hapur", "Në mesnatë, pronari u kthye në Zyrë dhe e gjeti kasafortën të hapur. Dosja ishte zhdukur. Askush nuk e di në cilën orë u mor." },
                new[] { "◉", "Gjashtë njerëz, tre kate", "Arta, Blerimi, Drita, Elmazi, Fatmiri dhe Genta ishin në vilë: në përdhesë, lart në katin e parë dhe poshtë në bodrum." },
                new[] { "!", "Rafti që duket i lëvizur", "Në Bibliotekë, një raft i vjetër është shtyrë pak nga muri. Kontrolloje me «Kërko»: mund të fshehë një rrugë që nuk shihet në hartë." },
                new[] { "★", "Dosja lëviz me hajnin", "Para vjedhjes, dosja ishte në Zyrë. Pas saj, shkon kudo që shkon hajni. Gjej kush e mori dhe në cilën orë, me jo më shumë se 16 pyetje." }
            },
            Epilogue = "Fatmiri, nipi i pronarit, tha se nuk kishte lëvizur nga Salloni. Në 21:00 hyri në Bibliotekë, shtyu raftin dhe në 22:00 doli në Zyrë nga kalimi i fshehtë. "
                     + "Mori dosjen dhe zbriti nëpër Korridor e Shkallë. Dosja u gjet në xhepin e palltos së tij."
        };

        public static Story For(CaseDefinition c)
        {
            if (c == null) return null;
            switch (c.Id)
            {
                case "karta3": return Karta3;
                case "karta4": return Karta4;
                default: return null;
            }
        }

        static string Key(CaseDefinition c)
        {
            return "alibi_story_" + c.Id;
        }

        // Hyrja shfaqet vetë herën e parë që hapet karta; pastaj vetëm me butonin «Rrëfimi».
        public static bool ShouldShow(CaseDefinition c)
        {
            return For(c) != null && PlayerPrefs.GetInt(Key(c), 0) == 0;
        }

        public static void MarkSeen(CaseDefinition c)
        {
            if (c == null) return;
            PlayerPrefs.SetInt(Key(c), 1);
            PlayerPrefs.Save();
        }
    }
}
