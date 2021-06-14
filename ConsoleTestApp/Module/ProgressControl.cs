using ConsoleTestApp;
using ConsoleTestApp.Module;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleTestApp.Module
{
	public enum InstructionType
	{
		Calculate,
		Input,
		Output
	}

	public enum Status
	{
		Ready,
		Running,
		Block,
		Finished
	}

	public class ProgressControlBlock
	{
		#region Members and CTOR
		public int Id { get; init; } = Common.Id;
		public string ProcessName
		{
			get;
			set;
		}

		//public int Priority { get; set; }
		public Queue<InstructionBase> InstructionQueue;

		public bool IsAlive { get; set; }
		public Status ProgressStatus { get; set; }

		/*
		public int ServeTime
		{
			get
			{
				int time = 0;
				foreach(var item in InstructionQueue)
				{
					if (item.Type is InstructionType.Calculate)
					{
						time += item.Time;
					}
				}
				return time;
			}
			set => ServeTime = value;
		}
		*/

		public ProgressControlBlock(Queue<InstructionBase> instructions, string processName = null)
		{
			InstructionQueue = instructions;

			if (string.IsNullOrEmpty(processName))
			{
				ProcessName = $"Process.{Id}";
			}
			else
			{
				ProcessName = processName;
			}

			IsAlive = false;
			ProgressStatus = Status.Ready;
		}
		#endregion

		public void Run(int timeSlice)
		{
			InstructionBase current = InstructionQueue.Peek();

			if(DoTick(current, timeSlice))
			{
				_ = InstructionQueue.Dequeue();
			}
		}

		public void RunFinish(string printOffset = "	")
		{
			InstructionBase current = InstructionQueue.Peek();

			int i = current.Time;
			do
			{
				Console.WriteLine($"{printOffset} Tick {i}");
				Thread.Sleep(Common.TICK);

				current.Time -= 1;
				i -= 1;

				if (current.Time <= 0)
				{
					break;
				}

			} while (i > 0);

			_ = InstructionQueue.Dequeue();
		}

		private static bool DoTick(InstructionBase currentInstrution, int timeSlice)
		{
			int i = timeSlice;
			do
			{
				Console.WriteLine($"Tick {i}");
				Thread.Sleep(Common.TICK);

				currentInstrution.Time -= 1;
				i -= 1;

				if (currentInstrution.Time <= 0)
				{
					return true;
				}

			} while (i > 0);

			return false;
		}
	}
}