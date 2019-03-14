using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public abstract class TypePatch : SymbolPatch, IType
	{
		public TypePath FullName { get; }


		public TypePatch(IType source)
			:base(source)
		{
			FullName = source.FullName;
		}

		public virtual void Filter(SymbolFilter remove)
		{
			//Do nothing by default.
		}


		public static TypePatch Create(IType source)
		{
			switch (source)
			{
			case IEnum t:
				return new EnumPatch(t);
			case IDelegate t:
				return new DelegatePatch(t);
			case IInterface t:
				return new InterfacePatch(t);
			case IStruct t:
				return new StructPatch(t);
			case IClass t:
				return new ClassPatch(t);
			default:
				throw new ArgumentException($"Unknown type: '{source.GetType().FullName}'", nameof(source));
			}
		}
	}
}
