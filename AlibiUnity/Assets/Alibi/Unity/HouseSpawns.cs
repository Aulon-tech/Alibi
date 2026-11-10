using System.Collections.Generic;
using Alibi.Core;
using UnityEngine;

namespace Alibi.Unity
{
    // Ku qëndron secili i dyshuar në vilë: një person për dhomë, jo të gjithë bashkë.
    // Pozicionet janë në koordinatat lokale të VillaView (të njëjtat me RoomLayout / Slot).
    public static class HouseSpawns
    {
        // Dhoma e "shtëpisë" për secilin nga 6 të dyshuarit (id e të dyshuarit -> id e dhomës).
        static readonly string[,] Homes =
        {
            { "A", "KU" }, // Arta, kuzhiniere        -> Kuzhina
            { "B", "DN" }, // Blerimi, shofer         -> Dhoma e ngrënies
            { "D", "KO" }, // Drita, sekretare        -> Korridori
            { "E", "BI" }, // Elmazi, kopshtar        -> Biblioteka
            { "F", "SA" }, // Fatmiri, nipi           -> Salloni
            { "G", "ZY" }  // Genta, ekonomiste       -> Zyra
        };

        // Pika e lirë brenda dhomës, (u, v) matur nga qoshja (X0, Z0) e dhomës.
        // Është në brezin e mesit, larg mureve, mobiljeve (HouseDressing) dhe rreshtave të Slot().
        static readonly Dictionary<string, Vector2> Spots = new Dictionary<string, Vector2>
        {
            { "HO", new Vector2(2.0f, 1.5f) },
            { "SA", new Vector2(2.8f, 1.5f) },
            { "BI", new Vector2(1.1f, 1.5f) },
            { "DN", new Vector2(0.75f, 1.5f) },
            { "KU", new Vector2(1.5f, 1.5f) },
            { "SH", new Vector2(2.65f, 0.5f) }, // majtas janë shkallët
            { "KO", new Vector2(1.8f, 1.5f) },
            { "ZY", new Vector2(2.0f, 1.5f) },
            { "BO", new Vector2(1.5f, 1.6f) }
        };

        // Id e dhomës ku qëndron i dyshuari. Pranon id-në ("A") ose emrin ("Arta"), pa dallim shkronjash.
        // Kthen null nëse i dyshuari nuk njihet.
        public static string RoomFor(string suspectId)
        {
            if (string.IsNullOrEmpty(suspectId)) return null;
            string id = null;
            foreach (var s in Villa.AllSuspects)
            {
                if (string.Equals(s.Id, suspectId, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.Name, suspectId, System.StringComparison.OrdinalIgnoreCase))
                {
                    id = s.Id;
                    break;
                }
            }
            if (id == null) return null;
            for (int i = 0; i < Homes.GetLength(0); i++) if (Homes[i, 0] == id) return Homes[i, 1];
            return null;
        }

        // Pozicion i lirë brenda dhomës (jo në mur, jo mbi mobilje), në koordinata lokale të vilës.
        public static Vector3 PositionIn(RoomLayout room)
        {
            Vector2 uv;
            if (!Spots.TryGetValue(room.Id, out uv)) uv = new Vector2((room.X1 - room.X0) * 0.5f, (room.Z1 - room.Z0) * 0.5f);
            return new Vector3(room.X0 + uv.x, room.Y, room.Z0 + uv.y);
        }

        // Dhoma (indeksi në c.Rooms) për secilin të dyshuar të rastit, një person për dhomë.
        // Nëse dhoma e tij nuk ekziston në këtë rast (p.sh. Drita pa Korridor në Kartën 1), merr
        // dhomën e parë të lirë të rastit. Dhoma ndahen vetëm nëse ka më pak dhoma se të dyshuar.
        public static int[] Assign(CaseDefinition c)
        {
            var result = new int[c.N];
            var used = new bool[c.RoomCount];
            var pending = new List<int>();
            for (int s = 0; s < c.N; s++)
            {
                string home = RoomFor(c.Suspects[s].Id);
                int r = home == null ? -1 : c.RoomIndex(home);
                if (r >= 0 && !used[r])
                {
                    result[s] = r;
                    used[r] = true;
                }
                else
                {
                    result[s] = -1;
                    pending.Add(s);
                }
            }
            int next = 0;
            foreach (int s in pending)
            {
                int r = -1;
                for (int k = 0; k < c.RoomCount && r < 0; k++) if (!used[k]) r = k;
                if (r < 0) r = (next++) % c.RoomCount;
                else used[r] = true;
                result[s] = r;
            }
            return result;
        }
    }
}
