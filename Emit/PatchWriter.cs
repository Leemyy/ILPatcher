using System.Collections.Generic;
using System.IO;
using ILPatcher.Patches;

namespace ILPatcher.Emit
{
	public static class PatchWriter
	{
		private static readonly char[] Forbidden = Path.GetInvalidFileNameChars();
		

		public static void Create(Patchwork patch, DirectoryInfo dir)
		{
			dir.Create();
			foreach (var space in patch.Namespaces)
			{
				if (space.Name == string.Empty)
					EmitNamespace(space, string.Empty, dir);
				else
					EmitNamespace(space, string.Empty, dir.CreateSubdirectory(space.Name));
			}
		}

		private static void EmitNamespace(NamespacePatch space, string preamble, DirectoryInfo dir)
		{
			foreach (var type in space.Types)
			{
				string filename = type.Name;
				filename = FixInvalidChars(filename);
				using (var cursor =
					new Cursor(Path.Combine(dir.FullName, filename + ".nsp")))
				{
					if (space.Name.Length > 0)
					{
						cursor.Write("namespace ");
						cursor.Write(preamble);
						cursor.WriteLine(space.Name);
						cursor.Spacer(1);
					}
					EmitType(type, cursor);
				}
			}
			foreach (var subSpace in space.SubSpaces)
			{
				EmitNamespace(
					subSpace,
					preamble + space.Name + ".",
					dir.CreateSubdirectory(subSpace.Name)
				);
			}
		}

		private static string FixInvalidChars(string filename)
		{
			if (filename.IndexOfAny(Forbidden) < 0)
				return filename;
			var chars = filename.ToCharArray();
			for (int i = 0; i < chars.Length; i++)
			{
				for (int c = 0; c < Forbidden.Length; c++)
					if (chars[i] == Forbidden[c])
						chars[i] = '#';
			}
			return new string(chars);
		}

		private static void EmitType(TypePatch type, Cursor cursor)
		{
			//if (type.CompilerGenerated)
			//	return;
			//if (type.IsSpecialName)
			//	cursor.WriteLine("#special");
			//cursor.Write('#');
			//cursor.WriteLine(type.Attributes);
			switch (type)
			{
			case ClassPatch t:
				EmitClass(t, cursor);
				break;
			case InterfacePatch t:
				EmitInterface(t, cursor);
				break;
			case DelegatePatch t:
				EmitDelegate(t, cursor);
				break;
			case StructPatch t:
				EmitStruct(t, cursor);
				break;
			case EnumPatch t:
				EmitEnum(t, cursor);
				break;
			}
		}

		private static void EmitEnum(EnumPatch type, Cursor cursor)
		{
			cursor.Write("enum ");
			cursor.Write(type.Name);
			cursor.WriteLine('{');
			cursor.Indent();
			bool consecutive = false;
			foreach (var constant in type.Constants)
			{
				if (consecutive)
					cursor.WriteLine(',');
				else
					consecutive = true;
				cursor.Write(constant.Name);
			}
			cursor.WriteLine();
			cursor.Unindent();
			cursor.WriteLine('}');
		}

		private static void EmitDelegate(DelegatePatch type, Cursor cursor, params string[] s)
		{
			cursor.Write("delegate ");
			cursor.Write(type.Name);
			if (type.GenericParameters.Count > 0)
			{
				cursor.Write(' ');
				EmitGenerics(type.GenericParameters, cursor);
			}
			EmitParameters(type.Parameters, cursor);
			cursor.Write(" : ");
			cursor.WriteLine(type.ReturnType);
		}

		private static void EmitInterface(InterfacePatch type, Cursor cursor)
		{
			cursor.Write("interface ");
			cursor.Write(type.Name);
			if (type.GenericParameters.Count > 0)
			{
				cursor.Write(' ');
				EmitGenerics(type.GenericParameters, cursor);
			}
			cursor.WriteLine();
			cursor.WriteLine('{');
			cursor.Indent();
			EmitProperties(type, cursor);
			cursor.Spacer(2);
			EmitMethods(type, cursor);
			cursor.Unindent();
			cursor.WriteLine('}');
		}

		private static void EmitStruct(StructPatch type, Cursor cursor)
		{
			cursor.Write("struct ");
			cursor.Write(type.Name);
			if (type.GenericParameters.Count > 0)
			{
				cursor.Write(' ');
				EmitGenerics(type.GenericParameters, cursor);
			}
			cursor.WriteLine();
			cursor.WriteLine('{');
			cursor.Indent();
			bool hasNested = false;
			foreach (var nested in type.Nested)
			{
				hasNested = true;
				EmitType(nested, cursor);
				cursor.Spacer(1);
			}
			if (hasNested)
				cursor.Spacer(2);
			EmitFields(type, cursor);
			cursor.Spacer(1);
			EmitProperties(type, cursor);
			cursor.Spacer(2);
			EmitMethods(type, cursor);
			cursor.Unindent();
			cursor.WriteLine('}');
		}

		private static void EmitClass(ClassPatch type, Cursor cursor)
		{
			cursor.Write("class ");
			cursor.Write(type.Name);
			if (type.GenericParameters.Count > 0)
			{
				cursor.Write(' ');
				EmitGenerics(type.GenericParameters, cursor);
			}
			cursor.WriteLine();
			cursor.WriteLine('{');
			cursor.Indent();
			bool hasNested = false;
			foreach (var nested in type.Nested)
			{
				hasNested = true;
				EmitType(nested, cursor);
				cursor.Spacer(1);
			}
			if (hasNested)
				cursor.Spacer(2);
			EmitFields(type, cursor);
			cursor.Spacer(1);
			EmitProperties(type, cursor);
			cursor.Spacer(2);
			//cursor.WriteLine("#Methods");
			EmitMethods(type, cursor);
			cursor.Unindent();
			cursor.WriteLine('}');
		}

		private static void EmitGenerics(IEnumerable<GenericParameterPatch> parameters, Cursor cursor)
		{
			cursor.Write('<');
			cursor.Indent();
			bool consecutive = false;
			foreach (var param in parameters)
			{
				if (consecutive)
					cursor.Write(", ");
				else
					consecutive = true;
				//cursor.WriteLine();
				if (param.Variance == Model.GenericVariance.Covariant)
					cursor.Write("out ");
				else if (param.Variance == Model.GenericVariance.Contravariant)
					cursor.Write("in ");
				cursor.Write(param.Name);
			}
			//cursor.WriteLine();
			cursor.Unindent();
			cursor.Write('>');
		}

		private static void EmitParameters(IReadOnlyCollection<ParameterPatch> parameters, Cursor cursor, bool extension = false)
		{
			cursor.Write('(');
			if (parameters.Count > 0)
			{
				if (extension)
					cursor.Write(" this");
				cursor.Indent();
				bool consecutive = false;
				foreach (var param in parameters)
				{
					if (consecutive)
						cursor.Write(',');
					else
						consecutive = true;
					cursor.WriteLine();
					cursor.Write(param.Name);
					if (param.IsOptional)
						cursor.Write(" :? ");
					else
						cursor.Write(" : ");
					if (param.Policy == Model.ParameterPolicy.Reference)
						cursor.Write("ref ");
					else if (param.Policy == Model.ParameterPolicy.In)
						cursor.Write("in ");
					else if (param.Policy == Model.ParameterPolicy.Out)
						cursor.Write("out ");
					else if (param.Policy == Model.ParameterPolicy.Params)
						cursor.Write("params ");
					cursor.Write(param.Type);
				}
				cursor.WriteLine();
				cursor.Unindent();
			}
			cursor.Write(')');
		}

		private static void EmitFields(DataTypePatch type, Cursor cursor)
		{
			foreach (var field in type.Fields)
			{
				cursor.Write(field.Name);
				cursor.Write(" : ");
				cursor.WriteLine(field.Type);
			}
		}

		private static void EmitProperties(MemberTypePatch type, Cursor cursor)
		{
			foreach (var property in type.Properties)
			{
				cursor.Write(property.Name);
				cursor.Write(" { ");
				if (property.HasGet)
					cursor.Write("get; ");
				if (property.HasSet)
					cursor.Write("set; ");
				cursor.Write("} : ");
				cursor.WriteLine(property.Type);
			}
		}

		private static void EmitMethods(MemberTypePatch type, Cursor cursor)
		{
			foreach (var method in type.Methods)
			{
				//if (method.IsSpecialName | method.IsRuntimeName)
				//{
				//	cursor.Write('#');
				//	if (method.IsRuntimeName)
				//		cursor.Write("runtime ");
				//	if (method.IsSpecialName)
				//		cursor.WriteLine("special");
				//}
				if (method.IsConstructor)
					cursor.Write(type.Name);
				else
					cursor.Write(method.Name);
				if (method.GenericParameters.Count > 0)
				{
					cursor.Write(' ');
					EmitGenerics(method.GenericParameters, cursor);
				}
				EmitParameters(method.Parameters, cursor, method.IsExtension);
				cursor.Write(" : ");
				cursor.WriteLine(method.ReturnType);
				cursor.Spacer(1);
			}
		}
	}
}
