using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class MethodHandle : MemberHandle, IMethod
	{
		private readonly List<GenericParameterHandle> _genericParameters;
		private readonly List<ParameterHandle> _parameters;

		public IReadOnlyList<GenericParameterHandle> GenericParameters => _genericParameters.AsReadOnly();
		public IReadOnlyList<ParameterHandle> Parameters => _parameters.AsReadOnly();

		public override string Identifier { get; }
		public bool IsSpecialName { get; }
		public bool IsRuntimeName { get; }
		public TypeLiteral ReturnType { get; }
		public int OptionalParameters { get; }
		public bool IsConstructor { get; }
		public bool IsOverride { get; }
		public bool IsGetter { get; }
		public bool IsSetter { get; }
		public bool IsExtension { get; }

		IEnumerable<IGenericParameter> IMethod.GenericParameters => GenericParameters;
		IEnumerable<IParameter> IMethod.Parameters => Parameters;


		public MethodHandle(MethodDefinition method)
			: base(method)
		{
			ReturnType = TypeLiteral.Parse(method.ReturnType);
			IsOverride = method.HasOverrides || method.IsVirtual && method.IsReuseSlot;
			IsConstructor = method.IsConstructor;
			IsGetter = method.IsGetter;
			IsSetter = method.IsSetter;
			IsSpecialName = method.IsSpecialName;
			IsRuntimeName = method.IsRuntimeSpecialName;
			if (method.HasGenericParameters)
			{
				var parameters = method.GenericParameters;
				_genericParameters = new List<GenericParameterHandle>(parameters.Count);
				for (int i = 0; i < parameters.Count; i++)
				{
					_genericParameters.Add(new GenericParameterHandle(parameters[i]));
				}
			}
			else
				_genericParameters = new List<GenericParameterHandle>();

			if (method.HasParameters)
			{
				IsExtension =
					method.IsStatic &&
					method.HasCustomAttributes &&
					method.CustomAttributes.Any(
						a => a.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute");
				var parameters = method.Parameters;
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
				_parameters = new List<ParameterHandle>();

			Identifier = IdentifierFor(_parameters.Count);
		}

		public string IdentifierFor(int paramCount)
		{
			var text = new StringBuilder();
			text.Append(Name);
			if (_genericParameters.Count > 0)
			{
				text.Append('`');
				text.Append(_genericParameters.Count);
			}
			text.Append('(');
			for (int i = 0; i < paramCount; i++)
			{
				if (i > 0)
					text.Append(',');
				var param = _parameters[i];
				if (param.Policy == ParameterPolicy.Reference ||
					param.Policy == ParameterPolicy.In ||
					param.Policy == ParameterPolicy.Out)
				{
					text.Append('&');
				}
				text.Append(param.Type);
			}
			text.Append(')');
			return text.ToString();
		}
	}
}
