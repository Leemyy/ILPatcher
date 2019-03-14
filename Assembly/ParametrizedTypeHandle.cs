using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using ILPatcher.Model;


namespace ILPatcher.Assembly
{
	public abstract class ParametrizedTypeHandle : TypeHandle, IParametrizedType
	{
		private readonly List<GenericParameterHandle> _genericParameters;

		public IReadOnlyList<GenericParameterHandle> Generics => _genericParameters.AsReadOnly();
		public override string Name { get; }
		public override TypePath FullName { get; }
		public override string Identifier { get; }
		public int DefinedGenericParameters { get; }

		IEnumerable<IGenericParameter> IParametrizedType.GenericParameters => Generics;


		public ParametrizedTypeHandle(TypeDefinition type, TypePath @namespace)
			: base(type)
		{
			string name = type.Name;
			Identifier = name;
			if (type.HasGenericParameters)
			{
				int index = name.IndexOf('`');
				if (index > 0)
				{
					DefinedGenericParameters = int.Parse(name.Substring(index + 1));
					name = name.Substring(0, index);
					_genericParameters = new List<GenericParameterHandle>(DefinedGenericParameters);
					var parameters = type.GenericParameters;
					int offset = parameters.Count - DefinedGenericParameters;
					for (int i = 0; i < DefinedGenericParameters; i++)
					{
						_genericParameters.Add(new GenericParameterHandle(parameters[i + offset]));
					}
				}
				else
					_genericParameters = new List<GenericParameterHandle>();
			}
			else
				_genericParameters = new List<GenericParameterHandle>();

			Name = name;

			FullName = new TypePath(name, @namespace);
		}
	}
}
