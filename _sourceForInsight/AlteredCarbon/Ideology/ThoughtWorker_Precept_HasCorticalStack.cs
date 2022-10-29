﻿using RimWorld;
using Verse;

namespace AlteredCarbon
{
	public class ThoughtWorker_Precept_HasCorticalStack : ThoughtWorker_Precept
	{
		public override ThoughtState ShouldHaveThought(Pawn p)
		{
			return p.HasStack();
		}
	}
}

