﻿using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace AlteredCarbon
{
    public class Recipe_InstallFilledCorticalStack : Recipe_InstallCorticalStack
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return false;
        }
    }
    public class Recipe_InstallCorticalStack : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (thing is Pawn pawn && pawn.DevelopmentalStage != DevelopmentalStage.Adult)
            {
                return false;
            }
            return base.AvailableOnNow(thing, part);
        }
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            return MedicalRecipesUtility.GetFixedPartsToApplyOn(recipe, pawn, delegate (BodyPartRecord record)
            {
                if (!pawn.health.hediffSet.GetNotMissingParts().Contains(record))
                {
                    return false;
                }
                if (pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(record))
                {
                    return false;
                }
                return (!pawn.health.hediffSet.hediffs.Any((Hediff x) => x.Part == record && (x.def == recipe.addsHediff || !recipe.CompatibleWithHediff(x.def)))) ? true : false;
            });
        }
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (ingredient is CorticalStack c)
            {
                c.dontKillThePawn = true;
            }
            base.ConsumeIngredient(ingredient, recipe, map);
        }
        private void CopyAllPhysicalDataFrom(Pawn source, Pawn to)
        {
            to.health.hediffSet.hediffs = new List<Hediff>();
            foreach (var hediff in source.health.hediffSet.hediffs)
            {
                to.health.hediffSet.hediffs.Add(hediff);
            }
            to.health.hediffSet.DirtyCache();
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null)
            {
                if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    foreach (var i in ingredients)
                    {
                        if (i is CorticalStack c)
                        {
                            c.stackCount = 1;
                            Traverse.Create(c).Field("mapIndexOrState").SetValue((sbyte)-1);
                            GenPlace.TryPlaceThing(c, billDoer.Position, billDoer.Map, ThingPlaceMode.Near);
                        }
                    }
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
            }

            var thing = ingredients.Where(x => x is CorticalStack).FirstOrDefault();
            if (thing is CorticalStack corticalStack)
            {
                var hediff = HediffMaker.MakeHediff(recipe.addsHediff, pawn) as Hediff_CorticalStack;
                if (corticalStack.PersonaData.ContainsInnerPersona)
                {
                    //foreach (Pawn item in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive)
                    //{
                    //    if (item.needs != null && item.RaceProps.IsFlesh && item.needs.mood != null && PawnUtility.ShouldGetThoughtAbout(item, pawn))
                    //    {
                    //        Log.Message("pawn: " + OpinionOf("pawn: ", pawn, item));
                    //        Log.Message("pawn (rev): " + OpinionOf("pawn (rev): ", item, pawn));
                    //    }
                    //}
                    hediff.PersonaData = corticalStack.PersonaData;
                    
                    if (pawn.IsEmptySleeve())
                    {
                        corticalStack.PersonaData.OverwritePawn(pawn, corticalStack.def.GetModExtension<StackSavingOptionsModExtension>(), null);
                    }
                    else
                    {
                        var gender = pawn.gender;
                        var kindDef = pawn.kindDef;
                        var faction = pawn.Faction;
                        var dummyPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kindDef, faction, fixedGender: gender,
                            fixedBiologicalAge: pawn.ageTracker.AgeBiologicalYearsFloat, fixedChronologicalAge: pawn.ageTracker.AgeChronologicalYearsFloat));
                        var copy = new PersonaData();

                        corticalStack.PersonaData.ErasePawn(dummyPawn);
                        var sourceStack = hediff.PersonaData.sourceStack ?? AC_DefOf.VFEU_FilledCorticalStack;
                        copy.CopyPawn(pawn, sourceStack); // we create a copy of original pawn
                        copy.OverwritePawn(pawnToOverwrite: dummyPawn, null, original: pawn);
                        CopyAllPhysicalDataFrom(pawn, dummyPawn);
                        corticalStack.PersonaData.ErasePawn(pawn);

                        GenSpawn.Spawn(dummyPawn, pawn.Position, pawn.Map);
                        dummyPawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();

                        foreach (Pawn item in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive)
                        {
                            if (item.needs != null && item.RaceProps.IsFlesh && item.needs.mood != null && PawnUtility.ShouldGetThoughtAbout(item, dummyPawn))
                            {
                                item.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
                                //Log.Message("dummyPawn: " + OpinionOf("dummyPawn: ", dummyPawn, item));
                                //Log.Message("dummyPawn (rev): " + OpinionOf("dummyPawn (rev): ", item, dummyPawn));
                            }
                        }

                        Pawn_HealthTracker_NotifyPlayerOfKilled_Patch.pawnToSkip = dummyPawn;
                        dummyPawn.Kill(null, hediff);
                        dummyPawn.Corpse.DeSpawn();

                        corticalStack.PersonaData.OverwritePawn(pawn, corticalStack.def.GetModExtension<StackSavingOptionsModExtension>(), dummyPawn);
                    }

                    pawn.health.AddHediff(hediff, part);
                    AlteredCarbonManager.Instance.StacksIndex.Remove(corticalStack.PersonaData.pawnID);
                    AlteredCarbonManager.Instance.ReplaceStackWithPawn(corticalStack, pawn);
                    
                    var naturalMood = pawn.story.traits.GetTrait(TraitDefOf.NaturalMood);
                    var nerves = pawn.story.traits.GetTrait(TraitDefOf.Nerves);
                    
                    if ((naturalMood != null && naturalMood.Degree == -2)
                            || pawn.story.traits.HasTrait(TraitDefOf.BodyPurist)
                            || (nerves != null && nerves.Degree == -2))
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(AC_DefOf.VFEU_NewSleeveDouble);
                    }
                    else
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(AC_DefOf.VFEU_NewSleeve);
                    }
                    
                    if (corticalStack.PersonaData.diedFromCombat.HasValue && corticalStack.PersonaData.diedFromCombat.Value)
                    {
                        pawn.health.AddHediff(HediffMaker.MakeHediff(AC_DefOf.VFEU_SleeveShock, pawn));
                        corticalStack.PersonaData.diedFromCombat = null;
                    }
                    if (corticalStack.PersonaData.hackedWhileOnStack)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(AC_DefOf.VFEU_SomethingIsWrong);
                        corticalStack.PersonaData.hackedWhileOnStack = false;
                    }
                }
                else
                {
                    pawn.health.AddHediff(hediff, part);
                }

                if (AlteredCarbonManager.Instance.emptySleeves != null && AlteredCarbonManager.Instance.emptySleeves.Contains(pawn))
                {
                    AlteredCarbonManager.Instance.emptySleeves.Remove(pawn);
                }

                if (ModsConfig.IdeologyActive)
                {
                    var eventDef = DefDatabase<HistoryEventDef>.GetNamed("VFEU_InstalledCorticalStack");
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(eventDef, pawn.Named(HistoryEventArgsNames.Doer)));
                }
            }
        }
    }
}