using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class EnumConstantPatch : SymbolPatch, IEnumConstant
	{
		public object Value { get; }


		public EnumConstantPatch(IEnumConstant source)
			: base(source)
		{
			Value = source.Value;
		}
	}
}
