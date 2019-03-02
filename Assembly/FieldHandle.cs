using System;
using System.Collections.Generic;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class FieldHandle : MemberHandle, IField
	{
		public TypeLiteral Type { get; }
		public bool IsConstant { get; }


		public FieldHandle(FieldDefinition field)
			: base(field)
		{
			Type = TypeLiteral.Parse(field.FieldType);
			IsConstant = field.HasConstant;
		}
	}
}
