using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RoundRobinApp.Module;
using YamlDotNet.Serialization;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace RoundRobinApp
{
	/// <summary>
	/// 可用于自身或导航至 Frame 内部的空白页。
	/// </summary>

	public sealed partial class MainPage : Page
	{
		private static ObservableCollection<ProgressViewModel> readyList = new ObservableCollection<ProgressViewModel>();
		private static ObservableCollection<ProgressViewModel> inputList = new ObservableCollection<ProgressViewModel>();
		private static ObservableCollection<ProgressViewModel> outputList = new ObservableCollection<ProgressViewModel>();

		private bool isStarted = false;
		private bool isPaused = false;
		private static ManualResetEvent manualSwitch = new ManualResetEvent(true);

		private static CoreDispatcher dispatcher = Window.Current.Dispatcher;

		public MainPage()
		{
			this.InitializeComponent();

			var progresses = Generator.GetSomeProgress();

			AddEndReadyListView(readyList, progresses);

			Add(progresses);

			ListViewReady.ItemsSource = readyList;
			ListViewInput.ItemsSource = inputList;
			ListViewOutput.ItemsSource = outputList;

		}

		#region Menu

		private async void MenuFlyoutItem_Open_File_ClickAsync(object sender, RoutedEventArgs e)
		{
			var picker = new Windows.Storage.Pickers.FileOpenPicker
			{
				//ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
				SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
			};
			picker.FileTypeFilter.Add(".yml");

			StorageFile file = await picker.PickSingleFileAsync();

			if (file != null)
			{
				string text = await Windows.Storage.FileIO.ReadTextAsync(file);

				var deserializer = new DeserializerBuilder()
				.WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
				.Build();

				var order = deserializer.Deserialize<ProgressYAML[]>(text);

				var list = new List<ProcessControlBlock>();
				foreach (var one in order)
				{
					var queue = new Queue<InstructionBase>();

					foreach (var item in one.instructions)
					{
						switch (item.Type)
						{
							case Module.TypeYAML.Calcu:
								{
									queue.Enqueue(new CalcuateInstruction(item.Time));
									break;
								}
							case Module.TypeYAML.Input:
								{
									queue.Enqueue(new InputInstruction(item.Time));
									break;
								}
							case Module.TypeYAML.Output:
								{
									queue.Enqueue(new OutputInstruction(item.Time));
									break;
								}
						}
					}
					list.Add(new ProcessControlBlock(queue, one.name));
				}

				AddEndReadyListView(readyList, list);
				Add(list);
			}
			else
			{
				// Cancelled
			}
		}

		private async void MenuFlyoutItem_Save_File_As_ClickAsync(object sender, RoutedEventArgs e)
		{
			var savePicker = new Windows.Storage.Pickers.FileSavePicker()
			{
				SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
				SuggestedFileName = "New Save",
			};

			savePicker.FileTypeChoices.Add("Save File", new List<string>() { ".yml" });

			StorageFile file = await savePicker.PickSaveFileAsync();

			if (file != null)
			{
				// Prevent updates to the remote version of the file until
				// we finish making changes and call CompleteUpdatesAsync.
				CachedFileManager.DeferUpdates(file);

				// write to file

				/*
				var save = new ProgressYAML[]
				{
					new ProgressYAML
					{
						name = "Progress.1",
						instructions = new List<InstructionYAML>
						{
							new InstructionYAML(Module.Type.Calcu, 30),
							new InstructionYAML(Module.Type.Calcu, 30),
							new InstructionYAML(Module.Type.Input, 20),
							new InstructionYAML(Module.Type.Calcu, 20),
							new InstructionYAML(Module.Type.Output, 15),
							new InstructionYAML(Module.Type.Calcu, 5)
						}
					},
					new ProgressYAML
					{
						name = "Test2",
						instructions =  new List<InstructionYAML>
						{
							new InstructionYAML(Module.Type.Calcu, 20),
							new InstructionYAML(Module.Type.Input, 30),
							new InstructionYAML(Module.Type.Calcu, 20),
							new InstructionYAML(Module.Type.Input, 100),
							new InstructionYAML(Module.Type.Calcu, 10),
							new InstructionYAML(Module.Type.Calcu, 20),
							new InstructionYAML(Module.Type.Input, 30),
							new InstructionYAML(Module.Type.Calcu, 10)
						}
					},
					new ProgressYAML
					{
						name = "Somejshdgkjhsdg",
						instructions =  new List<InstructionYAML>
						{
							new InstructionYAML(Module.Type.Calcu, 30),
							new InstructionYAML(Module.Type.Calcu, 20),
							new InstructionYAML(Module.Type.Calcu, 10)
						}
					},
				};
				*/

				var current = readyQueue.ToArray();
				var save = PCBToYAML(current);

				var serializer = new SerializerBuilder()
					.WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
					.Build();
				var yaml = serializer.Serialize(save);

				await Windows.Storage.FileIO.WriteTextAsync(file, yaml);

				// Let Windows know that we're finished changing the file so
				// the other app can update the remote version of the file.
				// Completing updates may require Windows to ask for user input.
				Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
				if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
				{
					//this.textBlock.Text = "File " + file.Name + " was saved.";
				}
				else
				{
					//this.textBlock.Text = "File " + file.Name + " couldn't be saved.";
				}
			}
			else
			{
				//this.textBlock.Text = "Operation cancelled.";
			}
		}
		
		private void MenuFlyoutItem_Add_Random_Click(object sender, RoutedEventArgs e)
		{
			var progresses = Generator.GetSomeProgress();

			AddEndReadyListView(readyList, progresses);
			ListViewReady.ItemsSource = readyList;

			Add(progresses);
		}

		private async void MenuFlyoutItem_Delete_All_ClickAsync(object sender, RoutedEventArgs e)
		{
			await Task.Run(() =>
			{
				readyQueue.Clear();
				inputQueue.Clear();
				outputQueue.Clear();
				_ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					readyList.Clear();
					inputList.Clear();
					outputList.Clear();
				});
			});
		}

		private void MenuFlyoutItem_Exit_Click(object sender, RoutedEventArgs e)
		{
			CoreApplication.Exit();
		}

		private ProgressYAML[] PCBToYAML(ProcessControlBlock[] current)
		{
			var result = new ProgressYAML[current.Length];

			for (int i = 0; i < current.Length; i++)
			{
				var progress = new ProgressYAML()
				{
					name = current[i].ProcessName,
					instructions = new List<InstructionYAML>()
				};

				foreach (var item in current[i].InstructionQueue)
				{
					progress.instructions.Add(new InstructionYAML((Module.TypeYAML)(int)item.Type, item.Time));
				}

				result[i] = progress;
			}
			return result;
		}

		#endregion
		#region Toolbar

		private void Button_Start_Click(object sender, RoutedEventArgs e)
		{
			if (!isStarted)
			{
				manualSwitch.Set();
				isStarted = true;
				Start();
			}

			if (isPaused)
			{
				manualSwitch.Reset();
				SetIsStartedIndicator(false);
				isPaused = false;
			}
			else
			{
				manualSwitch.Set();
				SetIsStartedIndicator(true);
				isPaused = true;
			}
		}

		private void Slider_Tick_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			Common.Tick = (int)e.NewValue;
		}

		private void SetIsStartedIndicator(bool isPlaying)
		{
			if (isPlaying)
			{
				PlayIcon.Visibility = Visibility.Collapsed;
				PauseIcon.Visibility = Visibility.Visible;
				TextBlock_Start.Text = "Pause";
			}
			else
			{
				PlayIcon.Visibility = Visibility.Visible;
				PauseIcon.Visibility = Visibility.Collapsed;
				TextBlock_Start.Text = "Start";
			}
		}

		#endregion

		// TODO: 使用 ICollection<T> 接口
		public void AddEndReadyListView(ObservableCollection<ProgressViewModel> list , ProcessControlBlock[] progresses)
		{
			foreach (var item in progresses)
			{
				list.Add(new ProgressViewModel(item.Id, item.ProcessName));
			}
		}

		private void AddEndReadyListView(ObservableCollection<ProgressViewModel> readyList, List<ProcessControlBlock> list)
		{
			foreach (var item in list)
			{
				readyList.Add(new ProgressViewModel(item.Id, item.ProcessName));
			}
		}

		private static void Add(ObservableCollection<ProgressViewModel> list, ProgressViewModel item)
		{
			_ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				list.Add(item);
			});
		}
		
		public void Add(params ProcessControlBlock[] list)
		{
			if (list.Length > 0)
			{
				foreach (var item in list)
				{
					readyQueue.Enqueue(item);
					Count++;
				}
			}
		}
		private void Add(List<ProcessControlBlock> list)
		{
			if (list.Count > 0)
			{
				foreach (var item in list)
				{
					readyQueue.Enqueue(item);
					Count++;
				}
			}
		}
		private static void RemoveAtZero(ObservableCollection<ProgressViewModel> list)
		{
			_ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				list.RemoveAt(0);
			});
		}

		private static ConcurrentQueue<ProcessControlBlock> readyQueue = new ConcurrentQueue<ProcessControlBlock>();
		private static ConcurrentQueue<ProcessControlBlock> inputQueue = new ConcurrentQueue<ProcessControlBlock>();
		private static ConcurrentQueue<ProcessControlBlock> outputQueue = new ConcurrentQueue<ProcessControlBlock>();
		
		private static Thread cpu = new Thread(CPUWork) { Name = "CPU Thread" };
		private static Thread input = new Thread(InputWork) { Name = "Input Thread" };
		private static Thread output = new Thread(OutputWork) { Name = "Output Thread" };

		#region Controler

		public static int TimeSlice = Common.TimeSlice;
		public int Count;

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
			AutoResetEvent wait = new AutoResetEvent(false);
			while (true)
			{
				manualSwitch.WaitOne();
				while (!readyQueue.IsEmpty)
				{
					manualSwitch.WaitOne();
					if (readyQueue.TryDequeue(out var item))
					{
						item.IsAlive = true;

						RemoveAtZero(readyList);
						switch (item.CurrentInstruction.Type)
						{
							case InstructionType.Calculate:
								{
									CalculateWork(item);

									break;
								}
							case InstructionType.Input:
								{
									inputQueue.Enqueue(item);
									Add(inputList, item.ProcessViewModel);

									break;
								}
							case InstructionType.Output:
								{
									outputQueue.Enqueue(item);
									Add(outputList, item.ProcessViewModel);

									break;
								}
							default:
								{
									break;
								}
						}
					}
				}
				wait.WaitOne(TimeSlice);
			}
		}

		private static void CalculateWork(ProcessControlBlock process)
		{
			process.ProgressStatus = Status.Running;

			process.Run(TimeSlice);

			if (IsProgressDone(process))
			{
				Debug.WriteLine($"{process.ProcessName} calculate done");

				return;
			}

			readyQueue.Enqueue(process);
			Add(readyList, process.ProcessViewModel);

			process.ProgressStatus = Status.Ready;
		}

		private static void InputWork()
		{
			AutoResetEvent wait = new AutoResetEvent(false);
			while (true)
			{
				if (!inputQueue.IsEmpty)
				{
					manualSwitch.WaitOne();
					if (inputQueue.TryDequeue(out var item))
					{
						manualSwitch.WaitOne();

						item.ProgressStatus = Status.Block;

						item.RunFinish();

						// TODO: 使用Logger
						Debug.WriteLine($"{item.ProcessName} input done");
					
						RemoveAtZero(inputList);
						readyQueue.Enqueue(item);
						Add(readyList, item.ProcessViewModel);
	
						item.ProgressStatus = Status.Ready;
					}
				}
				wait.WaitOne(TimeSlice);
			}
		}

		private static void OutputWork()
		{
			AutoResetEvent wait = new AutoResetEvent(false);
			while (true)
			{
				manualSwitch.WaitOne();
				if (!outputQueue.IsEmpty)
				{
					if (outputQueue.TryDequeue(out var item))
					{
						manualSwitch.WaitOne();
						item.ProgressStatus = Status.Block;
						
						item.RunFinish();

						// TODO: 使用Logger
						Debug.WriteLine($"{item.ProcessName} output done");

						
						
						RemoveAtZero(outputList);
						readyQueue.Enqueue(item);
						Add(readyList, item.ProcessViewModel);
						item.ProgressStatus = Status.Ready;
					}
				}
				wait.WaitOne(TimeSlice);
			}
		}

		private static void WaitWork()
		{

		}

		private static bool IsProgressDone(ProcessControlBlock progress)
		{
			return progress.InstructionQueue.Count == 0;
		}
		#endregion
	}
}