﻿using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlteredCarbon;

[HotSwappable]
public class ScenPart_CorticalStack : ScenPart_PawnModifier
{
    private HediffDef stackHediff = AC_DefOf.VFEU_CorticalStack;


    public override string Summary(Scenario scen)
    {
        return "AC.ScenPart_PawnsHaveCorticalStack".Translate(this.context.ToStringHuman(), this.chance.ToStringPercent(), this.stackHediff.label).CapitalizeFirst();
    }

    public override void ModifyPawnPostGenerate(Pawn pawn, bool redressed)
    {
        if (this.stackHediff != null && !pawn.HasCorticalStack())
        {
            pawn.health.AddHediff(this.stackHediff);
        }
    }

    public override void DoEditInterface(Listing_ScenEdit listing)
    {
        Rect scenPartRect = listing.GetScenPartRect(this, RowHeight * 2f);

        if (Widgets.ButtonText(scenPartRect.TopPartPixels(RowHeight), this.stackHediff.LabelCap, true, true, true, null))
        {
            FloatMenuUtility.MakeMenu(PossibleHediffs(), (HediffDef hd) => hd.LabelCap, (HediffDef hd) => delegate() { this.stackHediff = hd; });
        }

        Rect rect2 = scenPartRect.BottomPartPixels(RowHeight);
        Rect rect3 = rect2.LeftPart(0.333f).Rounded();
        Rect rect4 = rect2.RightPart(0.666f).Rounded();
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(rect3, "chance".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.TextFieldPercent(rect4, ref this.chance, ref this.chanceBuf, 0f, 1f);
    }

    private IEnumerable<HediffDef> PossibleHediffs()
    {
        yield return AC_DefOf.VFEU_CorticalStack;
        yield return AC_DefOf.AC_ArchoStack;
    }

    public override bool TryMerge(ScenPart other)
    {
        ScenPart_CorticalStack otherForcedStack = other as ScenPart_CorticalStack;
        if (otherForcedStack != null && this.stackHediff == otherForcedStack.stackHediff)
        {
            this.chance = GenMath.ChanceEitherHappens(this.chance, otherForcedStack.chance);
            return true;
        }

        return false;
    }

    public override bool CanCoexistWith(ScenPart other)
    {
        if (this.stackHediff == null)
        {
            return true;
        }

        ScenPart_CorticalStack otherForcedStack = other as ScenPart_CorticalStack;
        return otherForcedStack == null || this.stackHediff == otherForcedStack.stackHediff;
    }

    public override void Randomize()
    {
        base.Randomize();
        this.stackHediff = AC_DefOf.VFEU_CorticalStack;
        this.context = PawnGenerationContext.PlayerStarter;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look<HediffDef>(ref this.stackHediff, "ScenPart_CorticalStackDef");
    }

    public override bool HasNullDefs()
    {
        return base.HasNullDefs() || this.stackHediff == null;
    }
}