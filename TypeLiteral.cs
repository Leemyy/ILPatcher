using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using ILPatcher.Syntax;

namespace ILPatcher
{
	public class TypeLiteral : IEquatable<TypeLiteral>
	{
		private static readonly TypeLiteral[] NoArgs = new TypeLiteral[0];
		private static readonly IReadOnlyList<TypeLiteral> NoArgsReadonly = System.Array.AsReadOnly(NoArgs);
		private static readonly TypeLiteral DefaultNamespace = new TypeLiteral("");
		private static readonly TypeLiteral SystemNamespace = new TypeLiteral("System");
		public static readonly TypeLiteral Null = new TypeLiteral("null");

		public readonly string Name;
		public readonly bool Absolute;
		public readonly TypeLiteral Parent;
		private readonly TypeLiteral[] _arguments;

		public bool HasParent => !(Parent is null);
		public int Count => _arguments.Length;
		public TypeLiteral this[int index] => _arguments[index];
		public IReadOnlyList<TypeLiteral> Arguments =>
			_arguments == NoArgs ? NoArgsReadonly : System.Array.AsReadOnly(_arguments);

		private TypeLiteral(string name, TypeLiteral parent, TypeLiteral[] arguments)
		{
			Absolute = false;
			Name = name;
			Parent = parent;
			if (arguments is null || arguments.Length == 0)
				_arguments = NoArgs;
			else
				_arguments = arguments;
		}

		private TypeLiteral(string name)
		{
			Absolute = true;
			Name = name;
			_arguments = NoArgs;
		}

		public static TypeLiteral CreateRoot(string name)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			return new TypeLiteral(name);
		}

		public static TypeLiteral Create(string name, TypeLiteral parent = null, TypeLiteral[] arguments = null)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			if (arguments is null || arguments.Length == 0)
				return new TypeLiteral(name, parent, null);

			var copy = new TypeLiteral[arguments.Length];
			for (int i = 0; i < arguments.Length; i++)
			{
				var arg = arguments[i];
				if (arg is null)
					throw new ArgumentException("The arguments array must not contain null", nameof(arguments));
				copy[i] = arg;
			}
			return new TypeLiteral(name, parent, copy);
		}


		public bool Equals(TypeLiteral other)
		{
			if (other is null)
				return false;
			if (_arguments.Length != other._arguments.Length)
				return false;
			if (Name != other.Name)
				return false;

			if (HasParent)
			{
				if (other.Absolute)
					return false;
				if (other.HasParent && !Parent.Equals(other.Parent))
					return false;
			}
			else if (Absolute && other.HasParent)
				return false;

			for (int i = 0; i < _arguments.Length; i++)
			{
				if (!_arguments[i].Equals(other._arguments[i]))
					return false;
			}
			return true;
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}

		public virtual StringBuilder WriteTo(StringBuilder text)
		{
			if (false)//(!(Parent is null) && !Parent.Equals(DefaultNamespace))
			{
				Parent.WriteTo(text);
				text.Append('.');
			}
			text.Append(Name);
			if (_arguments.Length == 0)
				return text;
			text.Append('<');
			for (int i = 0; i < _arguments.Length; i++)
			{
				if (i > 0)
					text.Append(", ");
				_arguments[i].WriteTo(text);
			}
			text.Append('>');
			return text;
		}

		#region Parsing
		public static TypeLiteral Parse(TypeReferenceNode type)
		{
			if (type == null)
				return Null;
			if (type is ArrayTypeNode arr)
				return new Array(Parse(arr.Enclosed), arr.Commas.Count);
			if (type is NullableTypeNode nul)
				return new Nullable(Parse(nul.Enclosed));
			if (type is TupleTypeNode tuple)
				return ParseTuple(tuple);

			if (type is TypeNameNode solo)
			{
				var builtin = TryParseBuiltin(solo);
				if (!(builtin is null))
					return builtin;
				return ParseSimple(solo, null);
			}

			return ParseQualified((QualifiedTypeNameNode)type);
		}

		private static TypeLiteral ParseQualified(QualifiedTypeNameNode type)
		{
			TypeLiteral parent;
			if (type.Left is QualifiedTypeNameNode qualified)
				parent = ParseQualified(qualified);
			else
				parent = ParseSimple((TypeNameNode)type.Left, null);

			return ParseSimple(type.Right, parent);
		}

		private static TypeLiteral ParseSimple(TypeNameNode type, TypeLiteral parent)
		{
			if (!type.HasGenerics)
				return new TypeLiteral(type.Typename.Name, parent, null);

			var argNode = type.Generics.Arguments;
			var arguments = new TypeLiteral[argNode.Count];
			for (int i = 0; i < arguments.Length; i++)
			{
				arguments[i] = Parse(argNode[i]);
			}
			return new TypeLiteral(type.Typename.Name, parent, null);
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

			var argumentLiterals = new TypeLiteral[argCount];
			for (int i = 0; i < argCount; i++)
			{
				argumentLiterals[i] = Parse(arguments[i + argumentOffset]);
			}
			argumentOffset += argCount;

			return new TypeLiteral(name, parent, argumentLiterals);
		}

		private static TypeLiteral ParseNamespace(string fullNamespace)
		{
			if (fullNamespace.Length == 0)
				return DefaultNamespace;
			var spaces = fullNamespace.Split('.');
			var current = new TypeLiteral(spaces[0]);
			for (int i = 1; i < spaces.Length; i++)
			{
				current = new TypeLiteral(spaces[i], current, null);
			}
			return current;
		}

		private static TypeLiteral TryParseBuiltin(TypeNameNode type)
		{
			if (type.HasGenerics)
				return null;

			string name = type.Typename.Name;
			switch (name)
			{
			case "void":
				return Builtin.@void;
			case "object":
				return Builtin.@object;
			case "string":
				return Builtin.@string;
			case "long":
				return Builtin.@long;
			case "ulong":
				return Builtin.@ulong;
			case "int":
				return Builtin.@int;
			case "uint":
				return Builtin.@uint;
			case "short":
				return Builtin.@short;
			case "ushort":
				return Builtin.@ushort;
			case "byte":
				return Builtin.@byte;
			case "sbyte":
				return Builtin.@sbyte;
			case "bool":
				return Builtin.@bool;
			case "char":
				return Builtin.@char;
			case "float":
				return Builtin.@float;
			case "double":
				return Builtin.@double;
			case "decimal":
				return Builtin.@decimal;
			default:
				return null;
			}
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

		private static TypeLiteral ParseTuple(TupleTypeNode tuple)
		{
			var elements = new TypeLiteral[tuple.Elements.Count];
			for (int i = 0; i < elements.Length; i++)
			{
				elements[i] = Parse(tuple.Elements[i]);
			}
			return new Tuple(elements);
		}

		private static TypeLiteral ParseTuple(GenericInstanceType tuple)
		{
			var arguments = tuple.GenericArguments;
			var elements = new TypeLiteral[arguments.Count];
			for (int i = 0; i < elements.Length; i++)
			{
				if (i == 7 && arguments[7] is GenericInstanceType end &&
					end.Namespace == "System" &&
					end.Name.StartsWith("ValueTuple`"))
				{
					arguments = end.GenericArguments;
					i = -1;
					continue;
				}
				elements[i] = Parse(arguments[i]);
			}
			return new Tuple(elements);
		}

		private static string NameOf(TypeReference type)
		{
			//Remove trailing '&' of ref type names
			if (type.IsByReference)
				return ((ByReferenceType)type).ElementType.Name;
			return type.Name;
		}
		#endregion

		public static bool operator ==(TypeLiteral left, TypeLiteral right)
		{
			if (left is null)
				return right is null;
			return left.Equals(right);
		}

		public static bool operator !=(TypeLiteral left, TypeLiteral right)
		{
			if (left is null)
				return !(right is null);
			return !left.Equals(right);
		}

		public override bool Equals(object other)
		{
			return Equals(other as TypeLiteral);
		}

		public override int GetHashCode()
		{
			//Ignore the Parent,
			// since otherwise shorthands would not
			// produce the same hash as qualified forms!
			//e.g.: B.C would have a different hash than A.B.C
			int hash = Name.GetHashCode();
			for (int i = 0; i < _arguments.Length; i++)
				hash ^= _arguments[i].GetHashCode();
			return hash;
		}


		public sealed class Builtin : TypeLiteral
		{
			public static readonly Builtin @void = new Builtin("void", "Void");
			public static readonly Builtin @object = new Builtin("object", "Object");
			public static readonly Builtin @string = new Builtin("string", "String");
			public static readonly Builtin @long = new Builtin("long", "Int64");
			public static readonly Builtin @ulong = new Builtin("ulong", "UInt64");
			public static readonly Builtin @int = new Builtin("int", "Int32");
			public static readonly Builtin @uint = new Builtin("uint", "UInt32");
			public static readonly Builtin @short = new Builtin("short", "Int16");
			public static readonly Builtin @ushort = new Builtin("ushort", "UInt16");
			public static readonly Builtin @byte = new Builtin("byte", "Byte");
			public static readonly Builtin @sbyte = new Builtin("sbyte", "SByte");
			public static readonly Builtin @bool = new Builtin("bool", "Boolean");
			public static readonly Builtin @char = new Builtin("char", "Char");
			public static readonly Builtin @float = new Builtin("float", "Single");
			public static readonly Builtin @double = new Builtin("double", "Double");
			public static readonly Builtin @decimal = new Builtin("decimal", "Decimal");

			public string Alias { get; }

			private Builtin(string name, string runtimeName)
				: base(runtimeName, SystemNamespace, null)
			{
				Alias = name;
			}

			public override string ToString()
			{
				return Alias;
			}

			public override StringBuilder WriteTo(StringBuilder text)
			{
				return text.Append(Alias);
			}
		}

		public sealed class Array : TypeLiteral
		{
			public int Rank { get; }
			public TypeLiteral ElementType => _arguments[0];

			public Array(TypeLiteral enclosed, int rank)
				: base("Array°"+rank, SystemNamespace, new TypeLiteral[] { enclosed })
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

		public sealed class Nullable : TypeLiteral
		{
			public TypeLiteral EnclosedType => _arguments[0];

			public Nullable(TypeLiteral enclosed)
				: base("Nullable", SystemNamespace, new TypeLiteral[] { enclosed })
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

		public sealed class Tuple : TypeLiteral
		{
			public Tuple(TypeLiteral[] elements)
				: base("ValueTuple", SystemNamespace, Copy(elements))
			{
				if (elements.Length < 1)
					throw new ArgumentException("A Tuple must have at least one element.", nameof(elements));
			}

			private static TypeLiteral[] Copy(TypeLiteral[] original)
			{
				var copy = new TypeLiteral[original.Length];
				original.CopyTo(copy, 0);
				return copy;
			}

			public override StringBuilder WriteTo(StringBuilder text)
			{
				text.Append('(');
				for (int i = 0; i < _arguments.Length; i++)
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
