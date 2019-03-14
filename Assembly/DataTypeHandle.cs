using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public abstract class DataTypeHandle : MemberTypeHandle, IDataType
	{
		private readonly List<TypeHandle> _nested;
		private readonly List<FieldHandle> _fields;

		public IReadOnlyList<TypeHandle> Nested => _nested.AsReadOnly();
		public IReadOnlyList<FieldHandle> Fields => _fields.AsReadOnly();

		IEnumerable<IType> IDataType.Nested => Nested;
		IEnumerable<IField> IDataType.Fields => Fields;


		public DataTypeHandle(TypeDefinition type, TypePath @namespace)
			: base(type, @namespace)
		{
			if (type.HasFields)
			{
				var fields = type.Fields;
				_fields = new List<FieldHandle>(fields.Count);
				for (int i = 0; i < fields.Count; i++)
				{
					var field = new FieldHandle(fields[i]);
					_fields.Add(field);
				}
			}
			else
				_fields = new List<FieldHandle>();

			if (!type.HasNestedTypes)
			{
				_nested = new List<TypeHandle>();
				return;
			}
			var nested = type.NestedTypes;
			_nested = new List<TypeHandle>(nested.Count);
			for (int i = 0; i < nested.Count; i++)
			{
				var nestedType = TypeHandle.Create(nested[i], null);
				_nested.Add(nestedType);
			}
		}
	}
}
