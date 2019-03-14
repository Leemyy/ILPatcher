using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class DelegatePatch : ParameterizedTypePatch, IDelegate
	{
		public List<ParameterPatch> Parameters { get; } = new List<ParameterPatch>();
		public TypeLiteral ReturnType { get; }
		
		IEnumerable<IParameter> IDelegate.Parameters => Parameters;


		public DelegatePatch(IDelegate source)
			: base(source)
		{
			foreach (var param in source.Parameters)
			{
				Parameters.Add(new ParameterPatch(param));
			}
			ReturnType = source.ReturnType;
		}
	}
}
