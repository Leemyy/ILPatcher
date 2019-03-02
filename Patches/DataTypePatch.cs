using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public abstract class DataTypePatch : MemberTypePatch, IDataType
	{
		public List<TypePatch> Nested { get; } = new List<TypePatch>();
		public List<FieldPatch> Fields { get; } = new List<FieldPatch>();

		IEnumerable<IType> IDataType.Nested => Nested;
		IEnumerable<IField> IDataType.Fields => Fields;


		public DataTypePatch(IDataType source, NamespacePatch namespc)
			: base(source, namespc)
		{
			foreach (var nested in source.Nested)
			{
				Nested.Add(TypePatch.Create(nested, null));
			}
			foreach (var field in source.Fields)
			{
				Fields.Add(new FieldPatch(field));
			}
		}

		public override void Filter(SymbolFilter remove)
		{
			Nested.FilterWith(remove);
			for (int i = 0; i < Nested.Count; i++)
			{
				Nested[i].Filter(remove);
			}
			base.Filter(remove);
			Fields.FilterWith(remove);
		}
	}
}
