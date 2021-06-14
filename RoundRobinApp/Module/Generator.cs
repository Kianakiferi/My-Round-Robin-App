using System;
using System.Collections.Generic;

namespace RoundRobinApp.Module
{
	public static class Generator
	{
		public static ProcessControlBlock[] GetSomeProgress(int number = 5, int minTime = 5, int maxTime = 100)
		{
			if (number <= 0)
			{
				return default;
			}
			Random random = new Random();

			var list = new ProcessControlBlock[number];
			for (int i = 0; i < number; i++)
			{
				var instructions = new Queue<InstructionBase>();
				int count = random.Next(1, 10);
				for (int j = 0; j < count; j++)
				{
					instructions.Enqueue(GetRandomInstruction(minTime, maxTime));
				}
				instructions.Enqueue(new CalcuateInstruction(5));
				list[i] = new ProcessControlBlock(instructions);
			}
			return list;
		}

		private static InstructionBase GetRandomInstruction(int minTime, int maxTime)
		{
			int multiply = 1;
			Random random = new Random();
			switch (random.Next(1, 10))
			{
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 6:
					{
						return new CalcuateInstruction(random.Next(minTime, maxTime));
					}
				case 7:
				case 8:
					{
						return new InputInstruction(random.Next(minTime, maxTime) * multiply);
					}
				case 9:
				case 10:
					{
						return new OutputInstruction(random.Next(minTime, maxTime) * multiply);
					}
			}
			return new CalcuateInstruction();
		}
	}

}