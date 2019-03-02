using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public abstract class TypeHandle : MemberHandle, IType
	{
		private readonly TypeDefinition _essence;
		
		public NamespaceHandle Namespace { get; }
		public bool CompilerGenerated { get; }
		public bool IsSpecialName { get; }
		public bool IsRuntimeName { get; }
		public abstract TypeVariant Variant { get; }

		INamespace IType.Namespace => Namespace;


		protected TypeHandle(TypeDefinition type, NamespaceHandle namespc)
			: base(type)
		{
			_essence = type;
			Namespace = namespc;
			IsSpecialName = type.IsSpecialName;
			IsRuntimeName = type.IsRuntimeSpecialName;
			CompilerGenerated = type.CustomAttributes.Any(
				a => a.AttributeType.FullName ==
				"System.Runtime.CompilerServices.CompilerGeneratedAttribute");
		}

		public void MoveTo(string newNamespace)
		{
			if (_essence.IsNested)
				throw new InvalidOperationException("Nested types can't have a namespace.");
			_essence.Namespace = newNamespace;
		}


		public static TypeHandle Create(TypeDefinition type, NamespaceHandle namespc)
		{
			if (type.IsEnum)
				return new EnumHandle(type, namespc);
			if (type.IsValueType)
				return new StructHandle(type, namespc);
			if (type.IsInterface)
				return new InterfaceHandle(type, namespc);
			if (type.BaseType?.FullName == "System.MulticastDelegate")
				return new DelegateHandle(type, namespc);
			return new ClassHandle(type, namespc);
		}
	}
}
