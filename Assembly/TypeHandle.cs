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
		
		public abstract TypePath FullName { get; }
		public bool CompilerGenerated { get; }
		public bool IsSpecialName { get; }
		public bool IsRuntimeName { get; }
		public abstract TypeVariant Variant { get; }


		protected TypeHandle(TypeDefinition type)
			: base(type)
		{
			_essence = type;
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


		public static TypeHandle Create(TypeDefinition type, TypePath @namespace)
		{
			if (type.IsEnum)
				return new EnumHandle(type, @namespace);
			if (type.IsValueType)
				return new StructHandle(type, @namespace);
			if (type.IsInterface)
				return new InterfaceHandle(type, @namespace);
			if (type.BaseType?.FullName == "System.MulticastDelegate")
				return new DelegateHandle(type, @namespace);
			return new ClassHandle(type, @namespace);
		}
	}
}
