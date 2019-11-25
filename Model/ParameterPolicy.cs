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

	public static class ParameterPolicyHelper
	{
		public static bool IsByVal(this ParameterPolicy policy)
		{
			return
				policy == ParameterPolicy.Value ||
				policy == ParameterPolicy.Params;
		}

		public static bool IsByRef(this ParameterPolicy policy)
		{
			return
				policy == ParameterPolicy.Reference ||
				policy == ParameterPolicy.In ||
				policy == ParameterPolicy.Out;
		}
	}
}
