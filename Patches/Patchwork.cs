using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class Patchwork : IRoot
	{
		private Lookup<TypePath, TypePatch> _readonlyTypes;
		public Dictionary<TypePath, TypePatch> Types { get; } = new Dictionary<TypePath, TypePatch>();

		ILookup<TypePath, IType> IRoot.Types =>
			_readonlyTypes ?? (_readonlyTypes = new Lookup<TypePath, TypePatch>(Types));


		public Patchwork(IRoot root)
		{
			foreach (var type in root.Types.Values)
			{
				var typePatch = TypePatch.Create(type);
				Types.Add(typePatch.FullName, typePatch);
			}
		}

		public void Filter(SymbolFilter remove)
		{
			Types.FilterWith(remove);
			foreach (var type in Types.Values)
			{
				type.Filter(remove);
			}
		}
	}
}
