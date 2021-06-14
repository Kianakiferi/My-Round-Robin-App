using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleTestApp.Module
{
	public abstract class InstructionBase
	{
		public int Time;
		public InstructionType Type { get; init; }
	}

	public class CaculateInstruction : InstructionBase
	{
		public CaculateInstruction(int time = Common.DEFAULT_INSTRUCTION_TIME)
		{
			Type = InstructionType.Calculate;
			Time = time;
		}
	}

	public class InputInstruction : InstructionBase
	{
		public InputInstruction(int time = Common.DEFAULT_INSTRUCTION_TIME)
		{
			Type = InstructionType.Input;
			Time = time;
		}

	}

	public class OutputInstruction : InstructionBase
	{
		public OutputInstruction(int time = Common.DEFAULT_INSTRUCTION_TIME)
		{
			Type = InstructionType.Output;
			Time = time;
		}
	}
}
