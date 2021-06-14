using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoundRobinApp.Module
{
	public static class Common
	{
		
		public const int DEFAULT_INSTRUCTION_TIME = 20;

		public static int Tick = 20;

		//10 Tick
		public static int TimeSlice = 10;

		private static int _id = 0;
		public static int Id
		{
			get => _id++;
			set => _id = value;
		}
	}
}
