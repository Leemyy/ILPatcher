using System;
using System.Collections.Generic;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class PropertyHandle : MemberHandle, IProperty
	{
		private readonly List<ParameterHandle> _parameters;

		public TypeLiteral Type { get; }
		public bool HasGet { get; }
		public bool HasSet { get; }
		public int Parameters { get; }


		public PropertyHandle(PropertyDefinition prop)
			: base(prop)
		{
			Type = TypeLiteral.Parse(prop.PropertyType);
			HasGet = !(prop.GetMethod is null);
			HasSet = !(prop.SetMethod is null);
			if (prop.HasParameters)
			{
				var paramCount = prop.Parameters.Count;
				_parameters = new List<ParameterHandle>(paramCount);
				for (int i = 0; i < paramCount; i++)
				{
					_parameters.Add(new ParameterHandle(prop.Parameters[i]));
				}
			}
			else
			{
				_parameters = new List<ParameterHandle>();
			}
			Parameters = _parameters.Count;
		}
	}
}
