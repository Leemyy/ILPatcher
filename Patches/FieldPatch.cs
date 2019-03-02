using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class FieldPatch : SymbolPatch, IField
	{
		public TypeLiteral Type { get; }
		public bool IsConstant { get; }


		public FieldPatch(IField source)
			: base(source)
		{
			Type = source.Type;
			IsConstant = source.IsConstant;
		}
	}
}
