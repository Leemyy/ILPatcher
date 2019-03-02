using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class ClassPatch : DataTypePatch, IClass
	{
		public TypeLiteral BaseClass { get; }


		public ClassPatch(IClass source, NamespacePatch namespc)
			: base(source, namespc)
		{
			BaseClass = source.BaseClass;
		}
	}
}
