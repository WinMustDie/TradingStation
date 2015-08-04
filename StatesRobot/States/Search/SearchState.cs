﻿using System.Collections.Generic;
using System.Linq;
using StatesRobot.States.End;
using TradeTools;
using TradeTools.Events;
using Utils;
using Utils.Events;

namespace StatesRobot.States.Search	//TODO документация!!
{
	class SearchState : IState
	{
		private struct RootElement
		{
			public int CandleIndex { get; private set; }
			public LinkedList<int> Children { get; private set; }

			public RootElement(int candleIndex)
				: this()
			{
				CandleIndex = candleIndex;
				Children = null;
			}
		}

		private readonly LinkedList<RootElement> searchTree;
		private readonly List<Extremum> firstLongExtremums;
		private readonly List<Extremum> firstShortExtremums;

		public SearchState(RobotContext context)
		{
			searchTree = new LinkedList<RootElement>();
			firstLongExtremums = new List<Extremum>();
			firstShortExtremums = new List<Extremum>();
		}

		public ITradeEvent Process(RobotContext context, Candle candle)
		{
			ITradeEvent result = null; //TODO SearchInfoEvent
			int currentIndex = context.Candles.Count;

			if (!searchTree.Any())
			{
				searchTree.AddLast(new RootElement(currentIndex));
				return result;
			}
			
			var leftIter = searchTree.First;
			while (leftIter != null)
			{
				var leftCandle = context.Candles[leftIter.Value.CandleIndex];
				if (!leftIter.Value.Children.Any())
				{
					TryAppendMidCandle(context, candle, ref leftIter);
					continue;
				}

				var midIter = leftIter.Value.Children.First;
				while (midIter != null)
				{
					if (currentIndex - midIter.Value > context.MaxSkippedCandlesCount) //TODO currentIndex
					{
						midIter = leftIter.Value.Children.RemoveElement(midIter);
						continue;
					}

					result = TryAppendExtremum(context, leftCandle, context.Candles[midIter.Value], candle) ?? result;
					if (result is DealEvent)
						return result;

					midIter = midIter.Next;
				}

				if (currentIndex - leftIter.Value.CandleIndex <= context.MaxSkippedCandlesCount)
				{
					leftIter.Value.Children.AddLast(currentIndex);	//TODO нужна ли вобще свечка в параметрах??
				}
				leftIter = leftIter.Next;
			}

			return result;
		}

		private ITradeEvent TryAppendExtremum(RobotContext context, Candle leftCandle, Candle midCandle, Candle processedCandle)
		{
			int currentIndex = context.Candles.Count;

			if (!IsRightCandle(leftCandle, midCandle, processedCandle)) 
				return null;

			var isMinimum = IsMinimum(leftCandle, midCandle, processedCandle);
			if (isMinimum)
			{
				firstShortExtremums.Add(new Extremum(processedCandle.Close, currentIndex, processedCandle.DateTime, true));
			}
			else
			{
				firstLongExtremums.Add(new Extremum(processedCandle.Close, currentIndex, processedCandle.DateTime, false));
			}

			var extremum = TryGetSecondExtremum(isMinimum);
			if (extremum == null)
				return null;

			if (NeedToTrade(context, extremum))
				return new DealEvent(extremum.IsMinimum, processedCandle.Close, extremum);

			return new SecondExtremumEvent(extremum, firstLongExtremums, firstShortExtremums);
		}

		public ITradeEvent StopTrading(RobotContext context)
		{
			context.CurrentState = new EndState();
			return new EndEvent();
		}

		private void TryAppendMidCandle(RobotContext context, Candle candle, ref LinkedListNode<RootElement> leftCandleNode)
		{
			if (context.Candles.Count - leftCandleNode.Value.CandleIndex > context.MaxSkippedCandlesCount)
			{
				leftCandleNode = searchTree.RemoveElement(leftCandleNode);
			}
			else if (IsMidCandle(context.Candles[leftCandleNode.Value.CandleIndex], candle))
			{
				leftCandleNode.Value.Children.AddLast(context.Candles.Count);
			}
		}
		
		private Extremum TryGetSecondExtremum(bool isMinimum)
		{
			if ((isMinimum ? firstShortExtremums : firstLongExtremums).Count < 3)
				return null;

			var right = firstLongExtremums[firstLongExtremums.Count - 1];
			var mid = firstLongExtremums[firstLongExtremums.Count - 2];
			var left = firstLongExtremums[firstLongExtremums.Count - 3];
			if (isMinimum? (mid.Value < left.Value && mid.Value < right.Value) :
							(mid.Value > left.Value && mid.Value > right.Value))
				return new Extremum(mid.Value, right.CheckerIndex, mid.DateTime, isMinimum);

			return null;
		}

		private bool NeedToTrade(RobotContext context, Extremum secondExtremum)
		{
			return secondExtremum.IsMinimum == IsTrendLong(context.Candles);
		}

		private bool IsMidCandle(Candle leftCandle, Candle midCandle)
		{
			return !midCandle.IsOuterTo(leftCandle) && !midCandle.IsInnerTo(leftCandle);
		}

		private bool IsRightCandle(Candle leftCandle, Candle midCandle, Candle rightCandle)
		{
			return !rightCandle.IsOuterTo(midCandle) && !rightCandle.IsInnerTo(midCandle) &&
				(IsMinimum(leftCandle, midCandle, rightCandle) || IsMaximum(leftCandle, midCandle, rightCandle));
		}

		private bool IsMaximum(Candle left, Candle mid, Candle right)
		{
			return mid.High > left.High && mid.High > left.High && mid.Low >= left.Low && mid.Low >= right.Low;
		}

		private bool IsMinimum(Candle left, Candle mid, Candle right)
		{
			return mid.Low < left.Low && mid.Low < left.Low && mid.High <= left.High && mid.High <= right.High;
		}
		
		private bool IsTrendLong(List<Candle> candles)
		{
			return candles[candles.Count - 1].Close > candles.First().Open;
		}
	}
}
