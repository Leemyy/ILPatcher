using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class EventPatch : SymbolPatch, IEvent
	{
		public TypeLiteral Type { get; }
		public bool IsOverride { get; }


		public EventPatch(IEvent source)
			: base(source)
		{
			Type = source.Type;
			IsOverride = source.IsOverride;
		}
	}
}
