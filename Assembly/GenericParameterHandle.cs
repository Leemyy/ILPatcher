using System;
using System.Collections.Generic;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class GenericParameterHandle : MemberHandle, IGenericParameter
	{
		public GenericVariance Variance { get; }


		public GenericParameterHandle(GenericParameter parameter)
			: base(parameter)
		{
			if (parameter.IsCovariant)
				Variance = GenericVariance.Covariant;
			else if (parameter.IsContravariant)
				Variance = GenericVariance.Contravariant;
			else
				Variance = GenericVariance.Invariant;
		}
	}
}
