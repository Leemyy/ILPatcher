using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public abstract class SymbolPatch : ISymbol, IPatch
	{
		public string Name { get; }
		public string TargetName { get; private set; }
		public virtual string Identifier => Name;
		public bool IsUnspeakable { get; }


		public SymbolPatch(ISymbol source)
		{
			Name = source.Name;
			TargetName = source.Name;
			IsUnspeakable = source.IsUnspeakable;
		}


		public void Rename(string name)
		{
			TargetName = name;
		}
	}
}
