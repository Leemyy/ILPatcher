using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class ParameterPatch : SymbolPatch, IParameter
	{
		public TypeLiteral Type { get; }
		public ParameterPolicy Policy { get; }
		public bool IsOptional { get; }


		public ParameterPatch(IParameter source)
			: base(source)
		{
			Type = source.Type;
			Policy = source.Policy;
			IsOptional = source.IsOptional;
		}
	}
}
