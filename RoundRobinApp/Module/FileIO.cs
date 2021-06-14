using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoundRobinApp.Module
{
	public enum TypeYAML
	{
		Calcu,
		Input,
		Output
	}

	public class InstructionYAML
	{
		public Module.TypeYAML Type;
		public int Time;

		public InstructionYAML()
		{
			Type = default;
			Time = Common.DEFAULT_INSTRUCTION_TIME;
		}

		public InstructionYAML(Module.TypeYAML type, int time)
		{
			Type = type;
			Time = time;
		}
	}

	public class ProgressYAML
	{
		public string name;
		public List<InstructionYAML> instructions;
	}

	class FileIO
	{

	}
}
