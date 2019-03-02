using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class EnumHandle : TypeHandle, IEnum
	{
		private readonly TypeReference _baseType;
		private readonly List<EnumConstantHandle> _constants;

		public IReadOnlyList<EnumConstantHandle> Constants => _constants.AsReadOnly();
		public override TypeVariant Variant => TypeVariant.Enum;
		public TypeLiteral BaseType { get; }

		IEnumerable<IEnumConstant> IEnum.Constants => Constants;


		public EnumHandle(TypeDefinition type, NamespaceHandle namespc)
			: base(type, namespc)
		{
			_constants = new List<EnumConstantHandle>(type.Fields.Count - 1);
			foreach (var field in type.Fields)
			{
				// The value__ field is reserved;
				// It marks the enums base type (int by default).
				if (field.Name == "value__")
				{
					_baseType = field.FieldType;
					BaseType = TypeLiteral.Parse(_baseType);
					continue;
				}
				var constant = new EnumConstantHandle(field);
				_constants.Add(constant);
			}
		}
	}
}
