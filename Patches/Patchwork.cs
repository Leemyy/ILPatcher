using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class Patchwork : IRoot
	{
		public List<NamespacePatch> Namespaces { get; } = new List<NamespacePatch>();

		IEnumerable<INamespace> IRoot.Namespaces => Namespaces;


		public Patchwork(IRoot root)
		{
			foreach (var namespc in root.Namespaces)
			{
				Namespaces.Add(new NamespacePatch(namespc, null));
			}
		}

		public void Filter(SymbolFilter remove)
		{
			Namespaces.FilterWith(remove);
			for (int i = 0; i < Namespaces.Count; i++)
			{
				Namespaces[i].Filter(remove);
			}
		}
	}
}
