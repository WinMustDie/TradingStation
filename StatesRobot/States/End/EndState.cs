﻿using TradeTools;
using TradeTools.Events;
using Utils.Events;

namespace StatesRobot.States.End
{
	class EndState : IState
	{
		public ITradeEvent Process(RobotContext context, Candle candle)
		{
			return new EndEvent();
		}

		public ITradeEvent StopTrading(RobotContext context)
		{
			return new EndEvent();
		}
	}
}
