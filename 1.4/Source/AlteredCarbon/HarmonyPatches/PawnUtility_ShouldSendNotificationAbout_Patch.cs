﻿using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(PawnUtility), "ShouldSendNotificationAbout")]
    public static class PawnUtility_ShouldSendNotificationAbout_Patch
    {
        public static bool Prefix(Pawn p)
        {
            if (p.IsEmptySleeve())
            {
                Log.Message("Preventing ");
                return false;
            }
            return true;
        }
    }
}

