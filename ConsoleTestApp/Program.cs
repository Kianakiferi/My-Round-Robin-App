using ConsoleTestApp.Module;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleTestApp
{
	public static class Common
	{
		public const int TICK = 200;
		public const int TIME_SLICE = 10;
		public const int DEFAULT_INSTRUCTION_TIME = 20;

		private static int _id = 0;
		public static int Id
		{
			get => _id++;
			set => _id = value;
		}
	}

	public class CustomEventArgs : EventArgs
	{
		public CustomEventArgs(string message)
		{
			Message = message;
		}

		public string Message { get; set; }
	}

	class Program
	{
		public static int TimeSlice = Common.TIME_SLICE;

		//private static List<ProgressControlBlock> progressList = new List<ProgressControlBlock>();
		public static ConcurrentQueue<ProgressControlBlock> ReadyQueue = new();
		public static ConcurrentQueue<ProgressControlBlock> InputQueue = new();
		public static ConcurrentQueue<ProgressControlBlock> OutputQueue = new();

		private static Thread deviceControllerInput;
		private static Thread deviceControllerOutput;

		public event EventHandler<CustomEventArgs> RaiseCustomEvent;

		private static void NewSomeProgresssAndEnqueue()
		{
			Queue<InstructionBase> instructions1 = new Queue<InstructionBase>();
			instructions1.Enqueue(new CaculateInstruction(15));
			instructions1.Enqueue(new InputInstruction());
			instructions1.Enqueue(new CaculateInstruction(30));
			var progress1 = new ProgressControlBlock(instructions1, "P1");

			Queue<InstructionBase> instructions2 = new Queue<InstructionBase>();
			instructions2.Enqueue(new CaculateInstruction(20));
			instructions2.Enqueue(new InputInstruction());
			instructions2.Enqueue(new CaculateInstruction(10));

			var progress2 = new ProgressControlBlock(instructions2, "P2");

			ReadyQueue.Enqueue(progress1);
			ReadyQueue.Enqueue(progress2);
		}

		public static void Main(string[] args)
		{
			AutoResetEvent waitForInstrutionReady = new AutoResetEvent(false);

			deviceControllerInput = new Thread(InputWork)
			{
				Name = "Input Thread",
			};

			deviceControllerOutput = new Thread(OutputWork)
			{
				Name = "Output Thread"
			};

			deviceControllerInput.Start();
			deviceControllerOutput.Start();

			NewSomeProgresssAndEnqueue();

			while (true)
			{
				while (!ReadyQueue.IsEmpty)
				{
					if (ReadyQueue.TryDequeue(out var item))
					{
						Console.WriteLine($"Run Progress {item.ProcessName}");
						item.IsAlive = true;
						var current = item.InstructionQueue.Peek();
						{
							switch (current.Type)
							{
								case InstructionType.Calculate:
									{
										item.ProgressStatus = Status.Running;

										Console.WriteLine($"Progress {item.ProcessName} calculate instruction");

										item.Run(TimeSlice);

										Console.WriteLine($"Instruction ETA {current.Time}");

										if (IsProgressDone(item))
										{
											break;
										}

										item.ProgressStatus = Status.Ready;
										ReadyQueue.Enqueue(item);
										break;
									}
								case InstructionType.Input:
									{
										item.ProgressStatus = Status.Block;

										InputQueue.Enqueue(item);

										break;
									}
								case InstructionType.Output:
									{
										item.ProgressStatus = Status.Block;

										OutputQueue.Enqueue(item);

										break;
									}
							}
						}
					}
				}

				waitForInstrutionReady.WaitOne(TimeSlice);
			}
		}

		private static void InputWork()
		{
			AutoResetEvent waitForInstrutionInput = new AutoResetEvent(false);
			while (true)
			{
				if (!InputQueue.IsEmpty)
				{
					if (InputQueue.TryDequeue(out var item))
					{
						Console.WriteLine($"Progress {item.ProcessName} input instruction");

						item.RunFinish();

						Console.WriteLine($"Progress {item.ProcessName} input done");
						item.ProgressStatus = Status.Ready;
						ReadyQueue.Enqueue(item);
					}
				}

				waitForInstrutionInput.WaitOne(TimeSlice);
			}
		}

		private static void OutputWork()
		{
			AutoResetEvent waitForInstrutionOutput = new AutoResetEvent(false);
			while (true)
			{
				if (!OutputQueue.IsEmpty)
				{
					if (OutputQueue.TryDequeue(out var item))
					{
						Console.WriteLine($"Progress {item.ProcessName} output instruction");

						item.RunFinish("		");

						Console.WriteLine($"Progress {item.ProcessName} output done");
						item.ProgressStatus = Status.Ready;
						ReadyQueue.Enqueue(item);

					}
				}

				waitForInstrutionOutput.WaitOne(TimeSlice);
			}
		}

		protected virtual void OnRaiseCustomEvent(CustomEventArgs e)
		{

			// Make a temporary copy of the event to avoid possibility of
			// a race condition if the last subscriber unsubscribes
			// immediately after the null check and before the event is raised.
			EventHandler<CustomEventArgs> raiseEvent = RaiseCustomEvent;

			// Event will be null if there are no subscribers
			if (raiseEvent != null)
			{
				// Format the string to send inside the CustomEventArgs parameter
				e.Message += $" at {DateTime.Now}";

				// Call to raise the event.
				raiseEvent(this, e);
			}
		}

		private static bool IsProgressDone(ProgressControlBlock progress)
		{
			return progress.InstructionQueue.Count == 0;
		}
	}
}
