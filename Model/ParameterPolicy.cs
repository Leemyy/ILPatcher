using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILPatcher.Model
{
	public enum ParameterPolicy : byte
	{
		Value,
		Reference,
		In,
		Out,
		Params
	}
}
