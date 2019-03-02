using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public abstract class ParameterizedTypePatch : TypePatch, IParametrizedType
	{
		public override string Identifier { get; }
		public List<GenericParameterPatch> GenericParameters { get; } = new List<GenericParameterPatch>();

		IEnumerable<IGenericParameter> IParametrizedType.GenericParameters => GenericParameters;


		public ParameterizedTypePatch(IParametrizedType source, NamespacePatch namespc)
			: base(source, namespc)
		{
			foreach (var generic in source.GenericParameters)
			{
				GenericParameters.Add(new GenericParameterPatch(generic));
			}
			int paramCount = GenericParameters.Count;
			if (paramCount > 0)
				Identifier = Name + '`' + paramCount.ToString();
			else
				Identifier = Name;
		}
	}
}
