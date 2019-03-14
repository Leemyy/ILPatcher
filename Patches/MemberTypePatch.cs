using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public abstract class MemberTypePatch : ParameterizedTypePatch, IMemberType
	{
		public List<PropertyPatch> Properties { get; } = new List<PropertyPatch>();
		public List<EventPatch> Events { get; } = new List<EventPatch>();
		public List<MethodPatch> Methods { get; } = new List<MethodPatch>();

		IEnumerable<IProperty> IMemberType.Properties => Properties;
		IEnumerable<IEvent> IMemberType.Events => Events;
		IEnumerable<IMethod> IMemberType.Methods => Methods;


		public MemberTypePatch(IMemberType source)
			: base(source)
		{
			foreach (var property in source.Properties)
			{
				Properties.Add(new PropertyPatch(property));
			}
			foreach (var @event in source.Events)
			{
				Events.Add(new EventPatch(@event));
			}
			foreach (var method in source.Methods)
			{
				Methods.Add(new MethodPatch(method));
			}
		}

		public override void Filter(SymbolFilter remove)
		{
			Properties.FilterWith(remove);
			Events.FilterWith(remove);
			Methods.FilterWith(remove);
		}
	}
}
