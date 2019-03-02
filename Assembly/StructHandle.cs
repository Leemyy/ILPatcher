using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class StructHandle : DataTypeHandle, IStruct
	{
		public override TypeVariant Variant => TypeVariant.Struct;


		public StructHandle(TypeDefinition type, NamespaceHandle namespc)
			: base(type, namespc)
		{

		}
	}
}
