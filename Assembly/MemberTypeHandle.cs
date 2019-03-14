using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public abstract class MemberTypeHandle : ParametrizedTypeHandle, IMemberType
	{
		private readonly List<PropertyHandle> _properties;
		private readonly List<EventHandle> _events;
		private readonly List<MethodHandle> _methods;

		public IReadOnlyList<PropertyHandle> Properties => _properties.AsReadOnly();
		public IReadOnlyList<EventHandle> Events => _events.AsReadOnly();
		public IReadOnlyList<MethodHandle> Methods => _methods.AsReadOnly();

		IEnumerable<IProperty> IMemberType.Properties => Properties;
		IEnumerable<IEvent> IMemberType.Events => Events;
		IEnumerable<IMethod> IMemberType.Methods => Methods;


		public MemberTypeHandle(TypeDefinition type, TypePath @namespace)
			: base(type, @namespace)
		{
			if (type.HasProperties)
			{
				var properties = type.Properties;
				_properties = new List<PropertyHandle>(properties.Count);
				for (int i = 0; i < properties.Count; i++)
				{
					var prop = new PropertyHandle(properties[i]);
					_properties.Add(prop);
				}
			}
			else
				_properties = new List<PropertyHandle>();

			if (type.HasEvents)
			{
				var events = type.Events;
				_events = new List<EventHandle>(events.Count);
				for (int i = 0; i < events.Count; i++)
				{
					var evnt = new EventHandle(events[i]);
					_events.Add(evnt);
				}
			}
			else
				_events = new List<EventHandle>();

			if (type.HasMethods)
			{
				var methods = type.Methods;
				_methods = new List<MethodHandle>(methods.Count);
				for (int i = 0; i < methods.Count; i++)
				{
					var method = new MethodHandle(methods[i]);
					_methods.Add(method);
				}
			}
			else
				_methods = new List<MethodHandle>();
		}
	}
}
