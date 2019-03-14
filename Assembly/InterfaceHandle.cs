using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class InterfaceHandle : MemberTypeHandle, IInterface
	{
		public override TypeVariant Variant => TypeVariant.Interface;


		public InterfaceHandle(TypeDefinition type, TypePath @namespace)
			: base(type, @namespace)
		{
		}
	}
}
