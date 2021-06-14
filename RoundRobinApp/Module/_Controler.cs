using Microsoft.Extensions.Logging;
using RoundRobinApp.Module;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace RoundRobinApp.Module
{
	public class QueueChangeEventArgs : EventArgs
	{
		public InstructionType Type { get; set; }

		public QueueChangeEventArgs(InstructionType type)
		{
			Type = type;
		}
	}

	public class _Controler
	{
		public static int TimeSlice = Common.TimeSlice;
		[ThreadStatic] public int Count;

		private readonly ILogger<_Controler> _logger;
		public static event EventHandler<QueueChangeEventArgs> QueueChanged;

		public delegate void Del(InstructionType type);

		private static ConcurrentQueue<ProcessControlBlock> _readyQueue = new ConcurrentQueue<ProcessControlBlock>();
		private static ConcurrentQueue<ProcessControlBlock> _inputQueue = new ConcurrentQueue<ProcessControlBlock>();
		private static ConcurrentQueue<ProcessControlBlock> _outputQueue = new ConcurrentQueue<ProcessControlBlock>();

		private static Thread cpu = new Thread(CPUWork) { Name = "CPU Thread" };
		private static Thread input = new Thread(InputWork) { Name = "Input Thread" };
		private static Thread output = new Thread(OutputWork) { Name = "Output Thread" };

		private static CoreDispatcher dispatcher = Windows.UI.Xaml.Window.Current.Dispatcher;

		/*
		public Controler(ILogger<Controler> logger)
		{
			_logger = logger;
		}
		*/

		public void Add(params ProcessControlBlock[] list)
		{
			if (list.Length > 0)
			{
				foreach (var item in list)
				{
					_readyQueue.Enqueue(item);
					Count++;
				}
			}
		}

		public void Start()
		{
			// TODO: 事件通知
			// TODO: If thread is running

			if (!cpu.IsAlive)
			{
				cpu.Start();
			}
			if (!input.IsAlive)
			{
				input.Start();
			}
			if (!output.IsAlive)
			{
				output.Start();
			}
		}
	
		private static void CPUWork()
		{
			AutoResetEvent readyWait = new AutoResetEvent(false);
			while (true)
			{
				while (!_readyQueue.IsEmpty)
				{
					if (_readyQueue.TryDequeue(out var item))
					{
						// TODO: 使用Logger
						Debug.WriteLine($"Run Progress {item.ProcessName}");

						item.IsAlive = true;

						if (item.InstructionQueue.TryPeek(out var current))
						{

							switch (current.Type)
							{
								case InstructionType.Calculate:
									{
										item.ProgressStatus = Status.Running;

										item.Run(TimeSlice);
										
										if (IsProgressDone(item))
										{
											Debug.WriteLine($"{item.ProcessName} calculate done");
											break;
										}
										else
										{
											item.ProgressStatus = Status.Ready;
											_readyQueue.Enqueue(item);
										}

										
										break;
									}
								case InstructionType.Input:
									{
										item.ProgressStatus = Status.Block;

										_inputQueue.Enqueue(item);

										break;
									}
								case InstructionType.Output:
									{
										item.ProgressStatus = Status.Block;

										_outputQueue.Enqueue(item);

										break;
									}
							}
						}

						// TODO: 解决未空PCB 空指令队列问题
						/*
						else
						{

						}
						*/
					}
				}
				readyWait.WaitOne(TimeSlice);
			}
		}

		private static void InputWork()
		{
			AutoResetEvent inputWait = new AutoResetEvent(false);
			while (true)
			{
				if (!_inputQueue.IsEmpty)
				{
					if (_inputQueue.TryDequeue(out var item))
					{
						// TODO: 使用Logger
						//Debug.WriteLine($"Progress {item.ProcessName} input instruction");

						item.RunFinish();

						// TODO: 使用Logger
						Debug.WriteLine($"Progress {item.ProcessName} input done");
						item.ProgressStatus = Status.Ready;
						_readyQueue.Enqueue(item);
					}
				}

				inputWait.WaitOne(TimeSlice);
			}
		}

		private static void OutputWork()
		{
			AutoResetEvent outputWait = new AutoResetEvent(false);
			while (true)
			{
				if (!_outputQueue.IsEmpty)
				{
					if (_outputQueue.TryDequeue(out var item))
					{
						// TODO: 使用Logger
						//Debug.WriteLine($"Progress {item.ProcessName} output instruction");

						item.RunFinish();

						// TODO: 使用Logger
						Debug.WriteLine($"Progress {item.ProcessName} output done");
						item.ProgressStatus = Status.Ready;
						_readyQueue.Enqueue(item);

					}
				}

				outputWait.WaitOne(TimeSlice);
			}
		}

		public static void DelegateMethod(InstructionType type)
		{
			//OnQueueChanged(new QueueChangeEventArgs(InstructionType.Calculate));
			Debug.WriteLine($"{type.ToString()} Done ---------------------------------------------------");

		}

		protected virtual void OnQueueChanged(QueueChangeEventArgs e)
		{
			// Make a temporary copy of the event to avoid possibility of
			// a race condition if the last subscriber unsubscribes
			// immediately after the null check and before the event is raised.
			EventHandler<QueueChangeEventArgs> handler = QueueChanged;

			// Event will be null if there are no subscribers
			if (handler != null)
			{
				// Format the string to send inside the CustomEventArgs parameter
				e.Type = default;

				// Call to raise the event.
				handler(this, e);
			}
		}

		// TODO: 把判断内置 使用属性
		private static bool IsProgressDone(ProcessControlBlock progress)
		{
			return progress.InstructionQueue.Count == 0;
		}
	}

}