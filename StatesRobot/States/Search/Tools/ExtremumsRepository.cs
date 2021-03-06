﻿using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using TradeTools;
using Wintellect.PowerCollections;

namespace StatesRobot.States.Search.Tools
{
    [Synchronization]
	public class ExtremumsRepository : ContextBoundObject, IExtremumsRepository
    {
		private readonly OrderedSet<Extremum> firstMaximums;
		private readonly OrderedSet<Extremum> firstMinimums;

		private readonly OrderedSet<Extremum> secondMaximums;
		private readonly OrderedSet<Extremum> secondMinimums;

		public ICollection<Extremum> FirstMaximums => firstMaximums.AsReadOnly();
	    public ICollection<Extremum> FirstMinimums => firstMinimums.AsReadOnly();

        public ICollection<Extremum> SecondMaximums => secondMaximums.AsReadOnly();
        public ICollection<Extremum> SecondMinimums => secondMinimums.AsReadOnly();

        public ExtremumsRepository()
		{
			firstMaximums = new OrderedSet<Extremum>(CompareExtremums);
			firstMinimums = new OrderedSet<Extremum>(CompareExtremums);

			secondMaximums = new OrderedSet<Extremum>(CompareExtremums);
			secondMinimums = new OrderedSet<Extremum>(CompareExtremums);
		}

		public Extremum AddExtremum(Extremum extremum)
		{
			var firstExtremums = extremum.IsMinimum ? firstMinimums : firstMaximums;

			if (firstExtremums.Contains(extremum))
				return null;

			firstExtremums.Add(extremum);

			if (CompareExtremums(firstExtremums.GetLast(), extremum) != 0)
				return null;

			if (firstExtremums.Count < 3)
				return null;

			if (extremum.IsMinimum)
				return TryGetSecondMinimum();

			return TryGetSecondMaximum();
		}

		private Extremum TryGetSecondMaximum()
		{
			int last = firstMaximums.Count - 1;
			if (firstMaximums[last - 1].Value > firstMaximums[last].Value &&
					(firstMaximums[last - 1].Value > firstMaximums[last - 2].Value ||
					firstMaximums[last - 1].Value == firstMaximums[last - 2].Value &&
					last - 3 >= 0 && firstMaximums[last - 1].Value > firstMaximums[last - 3].Value))
				return SaveSecondExtremum(firstMaximums[last - 1], firstMaximums[last]);

			return null;
		}

		private Extremum TryGetSecondMinimum()
		{
			int last = firstMinimums.Count - 1;
			if (firstMinimums[last - 1].Value < firstMinimums[last].Value &&
					(firstMinimums[last - 1].Value < firstMinimums[last - 2].Value ||
					firstMinimums[last - 1].Value == firstMinimums[last - 2].Value &&
					last - 3 >= 0 && firstMinimums[last - 1].Value < firstMinimums[last - 3].Value))
				return SaveSecondExtremum(firstMinimums[last - 1], firstMinimums[last]);

			return null;
		}

		private Extremum SaveSecondExtremum(Extremum mid, Extremum right)
		{
			var extremum = new Extremum(mid.Value, right.CheckerIndex, mid.DateTime, mid.IsMinimum);
			(mid.IsMinimum ? secondMinimums : secondMaximums).Add(extremum);
			return extremum;
		}

		private static int CompareExtremums(Extremum left, Extremum right)
		{
			if (left.DateTime > right.DateTime)
				return 1;

			if (left.DateTime < right.DateTime)
				return -1;

			/*if (left.CheckerIndex > right.CheckerIndex)
				return 1;

			if (left.CheckerIndex < right.CheckerIndex)
				return -1;*/

			return 0;
		}
	}
}