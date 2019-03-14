using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class EnumPatch : TypePatch, IEnum
	{
		public TypeLiteral BaseType { get; }
		public List<EnumConstantPatch> Constants { get; } = new List<EnumConstantPatch>();

		IEnumerable<IEnumConstant> IEnum.Constants => Constants;


		public EnumPatch(IEnum source)
			: base (source)
		{
			BaseType = source.BaseType;
			foreach (var constant in source.Constants)
			{
				Constants.Add(new EnumConstantPatch(constant));
			}
		}
	}
}
