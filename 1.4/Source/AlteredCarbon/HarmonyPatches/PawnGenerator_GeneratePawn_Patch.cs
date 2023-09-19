﻿using System;
using System.Linq;
using HarmonyLib;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    public static class PawnGenerator_GeneratePawn_Patch
    {
        public static void Postfix(Pawn __result)
        {
            if (__result != null && __result.RaceProps.Humanlike)
            {
                var extension = __result.kindDef.GetModExtension<StackSpawnModExtension>();
                if (extension != null)
                {
                    extension.TryAddStack(__result);
                }

                foreach (var precept in __result.Ideo.PreceptsListForReading
                    .OrderByDescending(x => x.def.GetModExtension<StackSpawnModExtension>()
                    ?.chanceToSpawnWithStack > 0))
                {
                    extension = precept?.def.GetModExtension<StackSpawnModExtension>();
                    if (extension != null)
                    {
                        extension.TryAddStack(__result);
                    }
                }
            }
        }
    }
}

