﻿using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using StatesRobot;
using Tests.Tools;
using Tests.Tools.History;
using Tests.Tools.Printing;
using TradeTools.Events;
using Assert = NUnit.Framework.Assert;

namespace Tests
{
    [TestClass]
	public class StrategyTests
	{
		private void RunTest(TradeParams tradeParams, Configurator configurator, TradesPrinter printer)
		{
			tradeParams.Validate();

			var advisor = new TradeAdvisor(configurator.Repository.Days.First().FiveMins);
			var robot = new RobotContext(tradeParams, configurator.Factory, advisor);
			var results = configurator.GetTradeResults();

			//foreach (var day in repository.Days)
			foreach (var day in configurator.Repository.Days.Skip(1))    //TODO перебирать параметры по абсолютному значению, а затем искать регрессию одних на другие
			{
				foreach (var candle in day.FiveMins)
				{
					var dealEvent = robot.Process(candle) as DealEvent;
					if (dealEvent != null)
					{
						results.AddDeal(dealEvent.Deal);
					}
				}
				var ev = robot.StopTrading() as DealEvent;
				if (ev != null)
				{
					results.AddDeal(ev.Deal);
				}
				//printer.PrintDepoWithParamsName(tradeParams, results);

				Assert.That(results.DealsAreClosed);
				robot.Reset();
			}

			//File.WriteAllLines("out.txt", results.GetDepositSizes().Select(s => (s - 30000).ToString()));
			printer.AddRow(tradeParams, results);
		}
		[TestCase("Configs/main.xml")]
		[TestCase("Configs/test.xml")]
		public void MainTest(string filename)
		{
			var configurator = new Configurator(filename);
			var printer = configurator.Printer;

			configurator.Executor.Execute(tp => RunTest(tp, configurator, printer));
			printer.PrintTable("MainTest");
		}
	}
}
