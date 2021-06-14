using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoundRobinApp.Module
{
	public enum InstructionType
	{
		Calculate,
		Input,
		Output
	}

	public abstract class InstructionBase
	{
		public InstructionType Type { get; set; }
		public int Time;
	}

	public class CalcuateInstruction : InstructionBase
	{
		public CalcuateInstruction(int time = Common.DEFAULT_INSTRUCTION_TIME)
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
