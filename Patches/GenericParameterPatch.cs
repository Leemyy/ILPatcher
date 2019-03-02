using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class GenericParameterPatch : SymbolPatch, IGenericParameter
	{
		public GenericVariance Variance { get; }


		public GenericParameterPatch(IGenericParameter source)
			: base(source)
		{
			Variance = source.Variance;
		}
	}
}
