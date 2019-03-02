using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class NamespacePatch : SymbolPatch, INamespace
	{
		public NamespacePatch Parent { get; }
		public List<NamespacePatch> SubSpaces { get; } = new List<NamespacePatch>();
		public List<TypePatch> Types { get; } = new List<TypePatch>();

		INamespace INamespace.Parent => Parent;
		IEnumerable<INamespace> INamespace.SubSpaces => SubSpaces;
		IEnumerable<IType> INamespace.Types => Types;


		public NamespacePatch(INamespace source, NamespacePatch parent)
			: base(source)
		{
			Parent = parent;
			foreach (var type in source.Types)
			{
				Types.Add(TypePatch.Create(type, this));
			}
			foreach (var subspace in source.SubSpaces)
			{
				SubSpaces.Add(new NamespacePatch(subspace, this));
			}
		}

		public void Filter(SymbolFilter remove)
		{
			Types.FilterWith(remove);
			for (int i = 0; i < Types.Count; i++)
			{
				Types[i].Filter(remove);
			}
			SubSpaces.FilterWith(remove);
			for (int i = 0; i < SubSpaces.Count; i++)
			{
				SubSpaces[i].Filter(remove);
			}
		}
	}
}
