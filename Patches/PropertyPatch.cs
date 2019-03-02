using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class PropertyPatch : SymbolPatch, IProperty
	{
		public TypeLiteral Type { get; }
		public bool HasGet { get; }
		public bool HasSet { get; }


		public PropertyPatch(IProperty source)
			: base(source)
		{
			Type = source.Type;
			HasGet = source.HasGet;
			HasSet = source.HasSet;
		}
	}
}
