using System;
using System.Collections.Generic;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class EnumConstantHandle : MemberHandle, IEnumConstant
	{
		public object Value { get; }


		public EnumConstantHandle(FieldDefinition field)
			: base(field)
		{
			Value = field.Constant;
		}
	}
}
