using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class ClassHandle : DataTypeHandle, IClass
	{
		public TypeLiteral BaseClass { get; }
		public override TypeVariant Variant => TypeVariant.Class;


		public ClassHandle(TypeDefinition type, NamespaceHandle namespc)
			: base(type, namespc)
		{
			BaseClass = TypeLiteral.Parse(type.BaseType);
		}
	}
}
