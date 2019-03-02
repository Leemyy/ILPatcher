using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class DelegateHandle : ParametrizedTypeHandle, IDelegate
	{
		private readonly List<ParameterHandle> _parameters;

		public IReadOnlyList<ParameterHandle> Parameters => _parameters.AsReadOnly();
		public override TypeVariant Variant => TypeVariant.Delegate;
		public TypeLiteral ReturnType { get; }
		public int OptionalParameters { get; }

		IEnumerable<IParameter> IDelegate.Parameters => Parameters;


		public DelegateHandle(TypeDefinition type, NamespaceHandle namespc)
			: base (type, namespc)
		{
			var invoke = type.Methods.First(m => m.Name == "Invoke");
			ReturnType = TypeLiteral.Parse(invoke.ReturnType);
			if (invoke.HasParameters)
			{
				var parameters = invoke.Parameters;
				int optional = 0;
				_parameters = new List<ParameterHandle>(parameters.Count);
				for (int i = 0; i < parameters.Count; i++)
				{
					var param = new ParameterHandle(parameters[i]);
					_parameters.Add(param);
					if (param.IsOptional)
						optional++;
				}
				OptionalParameters = optional;
			}
			else
			{
				_parameters = new List<ParameterHandle>();
			}
		}
	}
}
