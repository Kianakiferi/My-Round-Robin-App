using System;
using System.Collections.Generic;
using System.Threading;

namespace RoundRobinApp.Module
{
	public enum Status
	{
		Ready,
		Running,
		Block,
		Finished
	}

	public class ProgressViewModel
	{
		public int Id;
		public string Name;

		public ProgressViewModel(int id, string name)
		{
			Id = id;
			Name = name;
		}
	}

	public class ProcessControlBlock
	{
		public int Id = Common.Id;
		public string ProcessName;

		public bool IsAlive { get; set; }
		public Status ProgressStatus { get; set; }
		public InstructionBase CurrentInstruction 
		{ 
			get 
			{
				if(InstructionQueue.TryPeek(out var current))
				{
					return current;
				}
				return default;
			} 
		}
		public ProgressViewModel ProcessViewModel { get => new ProgressViewModel(Id, ProcessName); }

		public Queue<InstructionBase> InstructionQueue;

		public ProcessControlBlock(Queue<InstructionBase> instructions, string processName = null)
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

		public void Run(int timeSlice)
		{
			InstructionBase current = InstructionQueue.Peek();

			if (DoTick(current, timeSlice))
			{
				_ = InstructionQueue.Dequeue();
			}
		}

		public void RunFinish()
		{
			InstructionBase current = InstructionQueue.Peek();

			DoTick(current , current.Time);

			_ = InstructionQueue.Dequeue();
		}

		private static bool DoTick(InstructionBase currentInstrution, int timeSlice)
		{
			// TODO: 验证 for() 能不能用
			int i = timeSlice;
			do
			{
				if (currentInstrution.Time <= 0)
				{
					return true;
				}

				Thread.Sleep(Common.Tick);

				currentInstrution.Time -= 1;
				i -= 1;

			} while (i > 0);

			return false;
		}

	}
}