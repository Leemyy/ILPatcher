using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ILPatcher
{
	public class TypeLiteral
	{
		private static readonly List<TypeLiteral> NoArgs = new List<TypeLiteral>();
		private static readonly TypeLiteral SystemNamespace = new TypeLiteral("System", null, null);
		public static readonly TypeLiteral Null = new TypeLiteral("null", null, null);

		private readonly List<TypeLiteral> _arguments;

		public string Name { get; }
		public TypeLiteral Parent { get; }
		public IReadOnlyList<TypeLiteral> Arguments => _arguments.AsReadOnly();

		private TypeLiteral(string name, TypeLiteral parent, List<TypeLiteral> arguments)
		{
			Name = name;
			Parent = parent;
			_arguments = arguments ?? NoArgs;
		}


		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}

		public virtual StringBuilder WriteTo(StringBuilder text)
		{
			if (false)//(!(Parent is null))
			{
				Parent.WriteTo(text);
				text.Append('.');
			}
			text.Append(Name);
			if (_arguments.Count == 0)
				return text;
			text.Append('<');
			for (int i = 0; i < _arguments.Count; i++)
			{
				if (i > 0)
					text.Append(", ");
				_arguments[i].WriteTo(text);
			}
			text.Append('>');
			return text;
		}


		public static TypeLiteral Parse(TypeReference type)
		{
			if (type == null)
				return Null;
			if (type.IsGenericParameter)
				return new TypeLiteral(type.Name, null, null);

			var builtin = TryParseBuiltin(type);
			if (!(builtin is null))
				return builtin;

			return Parse(type, null, out _);
		}

		private static TypeLiteral Parse(
			TypeReference type,
			Mono.Collections.Generic.Collection<TypeReference> arguments,
			out int argumentOffset)
		{
			string name = NameOf(type);
			int argCount = 0;
			int index = name.IndexOf('`');
			if (index > -1)
			{
				if (!int.TryParse(name.Substring(index + 1), out argCount))
					argCount = 0;
				name = name.Substring(0, index);
			}
			if (type.IsGenericInstance)
			{
				arguments = ((GenericInstanceType)type).GenericArguments;
			}

			TypeLiteral parent;
			argumentOffset = 0;
			if (type.IsNested)
				parent = Parse(type.DeclaringType, arguments, out argumentOffset);
			else
				parent = ParseNamespace(type.Namespace);

			var argumentLiterals = new List<TypeLiteral>(argCount);
			for (int i = 0; i < argCount; i++)
			{
				argumentLiterals.Add(Parse(arguments[i + argumentOffset]));
			}
			argumentOffset += argCount;

			return new TypeLiteral(name, parent, argumentLiterals);
		}

		private static TypeLiteral ParseNamespace(string fullNamespace)
		{
			if (fullNamespace.Length == 0)
				return null;
			var spaces = fullNamespace.Split('.');
			var current = new TypeLiteral(spaces[0], null, null);
			for (int i = 1; i < spaces.Length; i++)
			{
				current = new TypeLiteral(spaces[i], current, null);
			}
			return current;
		}

		private static TypeLiteral TryParseBuiltin(TypeReference type)
		{
			if (type.IsArray)
			{
				var arrType = ((ArrayType)type);
				return new Array(Parse(arrType.ElementType), arrType.Rank);
			}
			if (type.Namespace != "System")
				return null;

			string name = NameOf(type);
			switch (name)
			{
			case "Void":
				return Builtin.@void;
			case "Object":
				return Builtin.@object;
			case "String":
				return Builtin.@string;
			case "Int64":
				return Builtin.@long;
			case "UInt64":
				return Builtin.@ulong;
			case "Int32":
				return Builtin.@int;
			case "UInt32":
				return Builtin.@uint;
			case "Int16":
				return Builtin.@short;
			case "UInt16":
				return Builtin.@ushort;
			case "Byte":
				return Builtin.@byte;
			case "SByte":
				return Builtin.@sbyte;
			case "Boolean":
				return Builtin.@bool;
			case "Char":
				return Builtin.@char;
			case "Single":
				return Builtin.@float;
			case "Double":
				return Builtin.@double;
			case "Decimal":
				return Builtin.@decimal;
			case "Nullable`1":
				return new Nullable(Parse((type as GenericInstanceType).GenericArguments[0]));
			case "ValueTuple`1":
			case "ValueTuple`2":
			case "ValueTuple`3":
			case "ValueTuple`4":
			case "ValueTuple`5":
			case "ValueTuple`6":
			case "ValueTuple`7":
			case "ValueTuple`8":
				return ParseTuple((GenericInstanceType)type);
			default:
				return null;
			}
		}

		private static TypeLiteral ParseTuple(GenericInstanceType tuple)
		{
			var arguments = tuple.GenericArguments;
			var elements = new List<TypeLiteral>(arguments.Count);
			for (int i = 0; i < arguments.Count; i++)
			{
				if (i == 7 && arguments[7] is GenericInstanceType end &&
					end.Namespace == "System" &&
					end.Name.StartsWith("ValueTuple`"))
				{
					arguments = end.GenericArguments;
					i = -1;
					continue;
				}
				elements.Add(Parse(arguments[i]));
			}
			return new Tuple(elements);
		}

		private static string NameOf(TypeReference type)
		{
			string name = type.Name;
			//Remove trailing '&' of ref type names
			if (type.IsByReference)
				return name.Substring(0, name.Length - 1);
			return name;
		}


		public class Builtin : TypeLiteral
		{
			public static Builtin @void = new Builtin("void", "Void");
			public static Builtin @object = new Builtin("object", "Object");
			public static Builtin @string = new Builtin("string", "String");
			public static Builtin @long = new Builtin("long", "Int64");
			public static Builtin @ulong = new Builtin("ulong", "UInt64");
			public static Builtin @int = new Builtin("int", "Int32");
			public static Builtin @uint = new Builtin("uint", "UInt32");
			public static Builtin @short = new Builtin("short", "Int16");
			public static Builtin @ushort = new Builtin("ushort", "UInt16");
			public static Builtin @byte = new Builtin("byte", "Byte");
			public static Builtin @sbyte = new Builtin("sbyte", "SByte");
			public static Builtin @bool = new Builtin("bool", "Boolean");
			public static Builtin @char = new Builtin("char", "Char");
			public static Builtin @float = new Builtin("float", "Single");
			public static Builtin @double = new Builtin("double", "Double");
			public static Builtin @decimal = new Builtin("decimal", "Decimal");

			public string TrueName { get; }

			private Builtin(string name, string runtimeName)
				: base(runtimeName, SystemNamespace, null)
			{
				TrueName = name;
			}

			public override string ToString()
			{
				return TrueName;
			}

			public override StringBuilder WriteTo(StringBuilder text)
			{
				return text.Append(TrueName);
			}
		}

		public class Array : TypeLiteral
		{
			public int Rank { get; }
			public TypeLiteral ElementType => _arguments[0];

			public Array(TypeLiteral enclosed, int rank)
				: base("Array°"+rank, SystemNamespace, new List<TypeLiteral> { enclosed })
			{
				if (rank <= 0)
					throw new ArgumentException("Array rank must be greater than 0.", nameof(rank));
				Rank = rank;
			}

			public override StringBuilder WriteTo(StringBuilder text)
			{
				_arguments[0].WriteTo(text);
				text.Append('[');
				text.Append(',', Rank - 1);
				return text.Append(']');
			}
		}

		public class Nullable : TypeLiteral
		{
			public Nullable(TypeLiteral enclosed)
				: base("Nullable", SystemNamespace, new List<TypeLiteral> { enclosed })
			{
				if (enclosed is Nullable)
					throw new ArgumentException("Cannot have a Nullable of Nullable.");
				if (enclosed is Array)
					throw new ArgumentException("Cannot have a Nullable of an Array.");
			}

			public override StringBuilder WriteTo(StringBuilder text)
			{
				_arguments[0].WriteTo(text);
				return text.Append('?');
			}
		}

		public class Tuple : TypeLiteral
		{
			public Tuple(List<TypeLiteral> elements)
				: base("ValueTuple", SystemNamespace, elements)
			{
				if (elements.Count < 1)
					throw new ArgumentException("A Tuple must have at least one element.", nameof(elements));
			}

			public override StringBuilder WriteTo(StringBuilder text)
			{
				text.Append('(');
				for (int i = 0; i < _arguments.Count; i++)
				{
					if (i > 0)
						text.Append(", ");
					_arguments[i].WriteTo(text);
				}
				return text.Append(')');
			}
		}
	}
}
