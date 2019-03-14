using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public abstract class TypePatch : SymbolPatch, IType
	{
		public NamespacePatch Namespace { get; }

		INamespace IType.FullName => Namespace;


		public TypePatch(IType source, NamespacePatch namespc)
			:base(source)
		{
			Namespace = namespc;
		}

		public virtual void Filter(SymbolFilter remove)
		{
			//Do nothing by default.
		}


		public static TypePatch Create(IType source, NamespacePatch namespc)
		{
			switch (source)
			{
			case IEnum t:
				return new EnumPatch(t, namespc);
			case IDelegate t:
				return new DelegatePatch(t, namespc);
			case IInterface t:
				return new InterfacePatch(t, namespc);
			case IStruct t:
				return new StructPatch(t, namespc);
			case IClass t:
				return new ClassPatch(t, namespc);
			default:
				throw new ArgumentException("Unknown type", nameof(source));
			}
		}
	}
}
