using System;
using System.Collections.Generic;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class EventHandle : MemberHandle, IEvent
	{
		public TypeLiteral Type { get; }
		public bool IsOverride { get; }


		public EventHandle(EventDefinition evnt)
			: base(evnt)
		{
			Type = TypeLiteral.Parse(evnt.EventType);
			IsOverride = evnt.AddMethod.HasOverrides;
		}
	}
}
