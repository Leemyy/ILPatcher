using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class StructPatch : DataTypePatch, IStruct
	{
		public StructPatch(IStruct source, NamespacePatch namespc)
			: base(source, namespc)
		{
		}
	}
}
