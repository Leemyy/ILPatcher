using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class ParameterHandle : ISymbol, IParameter
	{
		private readonly ParameterReference _essence;

		public string Name { get; }
		public virtual string Identifier => Name;
		public bool IsUnspeakable { get; }
		public TypeLiteral Type { get; }
		public ParameterPolicy Policy { get; }
		public bool IsOptional { get; }


		public ParameterHandle(ParameterDefinition param)
		{
			_essence = param;
			Name = param.Name;
			var index = Name.IndexOf('<');
			IsUnspeakable = index >= 0 && Name.IndexOf('>') > index;
			Type = TypeLiteral.Parse(param.ParameterType);
			//param.IsReturnValue
			if (param.IsIn)
				Policy = ParameterPolicy.In;
			else if (param.IsOut)
				Policy = ParameterPolicy.Out;
			else if (param.ParameterType.IsByReference)
				Policy = ParameterPolicy.Reference;
			else if (param.HasCustomAttributes &&
				param.CustomAttributes.Any(
					a => a.AttributeType.FullName == "System.ParamArrayAttribute")
			)
				Policy = ParameterPolicy.Params;
			else
				Policy = ParameterPolicy.Value;
			IsOptional = param.IsOptional;
		}


		public void Rename(string name)
		{
			_essence.Name = name;
		}
	}
}
