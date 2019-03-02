using System;
using System.Collections.Generic;
using System.Text;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class MethodPatch : SymbolPatch, IMethod
	{
		public override string Identifier { get; }
		public List<GenericParameterPatch> GenericParameters { get; } = new List<GenericParameterPatch>();
		public List<ParameterPatch> Parameters { get; } = new List<ParameterPatch>();
		public TypeLiteral ReturnType { get; }
		public int OptionalParameters { get; }
		public bool IsConstructor { get; }
		public bool IsOverride { get; }
		public bool IsGetter { get; }
		public bool IsSetter { get; }
		public bool IsExtension { get; }

		IEnumerable<IGenericParameter> IMethod.GenericParameters => GenericParameters;
		IEnumerable<IParameter> IMethod.Parameters => Parameters;


		public MethodPatch(IMethod source)
			: base(source)
		{
			ReturnType = source.ReturnType;
			IsConstructor = source.IsConstructor;
			IsOverride = source.IsOverride;
			IsGetter = source.IsGetter;
			IsSetter = source.IsSetter;
			IsExtension = source.IsExtension;
			foreach (var generic in source.GenericParameters)
			{
				GenericParameters.Add(new GenericParameterPatch(generic));
			}
			int optional = 0;
			foreach (var param in source.Parameters)
			{
				if (param.IsOptional)
					optional++;
				Parameters.Add(new ParameterPatch(param));
			}
			OptionalParameters = optional;
			Identifier = IdentifierFor(Parameters.Count);
		}

		public string IdentifierFor(int paramCount)
		{
			var text = new StringBuilder();
			text.Append(Name);
			if (GenericParameters.Count > 0)
			{
				text.Append('`');
				text.Append(GenericParameters.Count);
			}
			text.Append('(');
			for (int i = 0; i < paramCount; i++)
			{
				if (i > 0)
					text.Append(',');
				var param = Parameters[i];
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
