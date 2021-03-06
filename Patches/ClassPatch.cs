using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class ClassPatch : DataTypePatch, IClass
	{
		public TypeLiteral BaseClass { get; }


		public ClassPatch(IClass source)
			: base(source)
		{
			BaseClass = source.BaseClass;
		}
	}
}
