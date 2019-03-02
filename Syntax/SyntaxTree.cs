using System.Collections.Generic;
using System.Collections.ObjectModel;
using ILPatcher.Emit;

namespace ILPatcher.Syntax
{
	public class SyntaxTree
	{
		public readonly NamespaceNode Namespace;
		public bool HasNamespace => !(Namespace is null);
		public readonly ReadOnlyCollection<TypeNode> Types;
		public readonly EndOfFileToken End;

		private SyntaxTree(List<TypeNode> contents, EndOfFileToken end, NamespaceNode @namespace)
			=> (Namespace, Types, End) = (@namespace, contents.AsReadOnly(), end);

		public static SyntaxTree Parse(Source source)
		{
			var @namespace = NamespaceNode.Parse(source);

			var types = new List<TypeNode>();
			while (true)
			{
				var end = source.ExpectEnd();
				if (!(end is null))
					return new SyntaxTree(types, end, @namespace);

				var type = TypeNode.Parse(source);

				//Add Parsed type
				if (!(type is null))
					types.Add(type);
				//..or skip one token, if parsing failed.
				else if (!source.SkipToken())
					return null;
			}
		}
	}

	public abstract class Node : SyntaxUnit
	{
		public readonly Span FullSpan;

		internal Node(Span span, Span fullSpan)
			: base(span)
		{
			FullSpan = fullSpan;
		}
	}

	public sealed class NamespaceNode
	{
		public readonly IdentifierToken Keyword;
		public readonly TypeReferenceNode Namespaces;

		private NamespaceNode(IdentifierToken keyword, TypeReferenceNode namespaces)
			=> (Keyword, Namespaces) = (keyword, namespaces);

		public static NamespaceNode Parse(Source source)
		{
			int pos = source.Position;
			var keyword = source.ExpectIdentifier("namespace");
			if (keyword is null)
				return null;
			
			var namespaces = TypeReferenceNode.Parse(source);
			if (namespaces is null && source.Reset(pos))
				return null;

			return new NamespaceNode(keyword, namespaces);
		}
	}

	public abstract class MemberNode
	{
		public readonly NameNode Name;

		internal MemberNode(NameNode name)
			=> Name = name;

		public static MemberNode Parse(Source source)
		{
			return
				FieldNode.Parse(source) ??
				PropertyNode.Parse(source) ??
				EventNode.Parse(source) ??
				MethodNode.Parse(source) ??
				(MemberNode) TypeNode.Parse(source);
		}
	}

	public abstract class TypeNode : MemberNode
	{
		public readonly IdentifierToken Keyword;
		public readonly TypeParameterListNode Generics;
		public bool HasGenerics => Generics != null;

		internal TypeNode(IdentifierToken keyword, NameNode name, TypeParameterListNode generics = null)
			: base(name)
			=> (Keyword, Generics) = (keyword, generics);

		public static new TypeNode Parse(Source source)
		{
			return
				InterfaceNode.Parse(source) ??
				DelegateNode.Parse(source) ??
				ClassNode.Parse(source) ??
				StructNode.Parse(source) ??
				(TypeNode) EnumNode.Parse(source);
		}
	}

	public sealed class EnumNode : TypeNode
	{
		public readonly ControlToken OpeningBracket;
		public readonly ReadOnlyCollection<NameNode> Constants;
		public readonly ReadOnlyCollection<ControlToken> Commas;
		public readonly ControlToken ClosingBracket;

		public EnumNode(IdentifierToken keyword, NameNode name,
			ControlToken open, List<NameNode> constants,
			List<ControlToken> commas, ControlToken close)
			: base(keyword, name)
			=> (OpeningBracket, Constants, Commas, ClosingBracket) = (open, constants.AsReadOnly(), commas.AsReadOnly(), close);

		public static new EnumNode Parse(Source source)
		{
			int pos = source.Position;
			var keyword = source.ExpectIdentifier("enum");
			if (keyword is null)
				return null;

			var name = NameNode.Parse(source);
			if (name is null && source.Reset(pos))
				return null;
			var open = source.ExpectControl('{');
			if (open is null && source.Reset(pos))
				return null;

			pos = source.Position;
			ControlToken close = null;
			var constants = new List<NameNode>();
			var commas = new List<ControlToken>();
			while (true)
			{
				close = source.ExpectControl('}');
				if (!(close is null))
					return new EnumNode(keyword, name, open, constants, commas, close);
				var first = NameNode.Parse(source);
				if (!(first is null))
				{
					constants.Add(first);
					break;
				}
				if (!source.SkipToken())
					return null;
			}
			bool expectName = false;
			ControlToken comma = null;
			while (true)
			{
				if (expectName)
				{
					var constant = NameNode.Parse(source);
					if (!(constant is null))
					{
						commas.Add(comma);
						constants.Add(constant);
						expectName = false;
						continue;
					}
					close = source.ExpectControl('}');
					if (!(close is null))
					{
						source.ParseError();
						commas.RemoveAt(commas.Count - 1);
						break;
					}
				}
				else
				{
					close = source.ExpectControl('}');
					if (!(close is null))
						break;
					comma = source.ExpectControl(',');
					if (!(comma is null))
					{
						expectName = true;
						continue;
					}
				}
				if (!source.SkipToken())
					return null;
			}

			return new EnumNode(keyword, name, open, constants, commas, close);
		}
	}

	public sealed class DelegateNode : TypeNode
	{
		public readonly ParameterListNode Parameters;
		public readonly ControlToken Colon;
		public readonly TypeReferenceNode ReturnType;

		public DelegateNode(IdentifierToken keyword, NameNode name, ParameterListNode parameters, ControlToken colon, TypeReferenceNode returnType, TypeParameterListNode generics = null)
			: base (keyword, name, generics)
			=> (Parameters, Colon, ReturnType) = (parameters, colon, returnType);


		public static new DelegateNode Parse(Source source)
		{
			int pos = source.Position;
			var keyword = source.ExpectIdentifier("delegate");
			if (keyword is null)
				return null;

			var name = NameNode.Parse(source);
			if (name is null && source.Reset(pos))
				return null;
			var generics = TypeParameterListNode.Parse(source);
			var parameters = ParameterListNode.Parse(source);
			if (parameters is null && source.Reset(pos))
				return null;

			var colon = source.ExpectControl(':');
			if (colon is null && source.Reset(pos))
				return null;
			var type = TypeReferenceNode.Parse(source);
			if (type is null && source.Reset(pos))
				return null;

			return new DelegateNode(keyword, name, parameters, colon, type, generics);
		}
	}

	public sealed class InterfaceNode : TypeNode
	{
		public readonly TypeBodyNode Body;

		public InterfaceNode(IdentifierToken keyword, NameNode name, TypeBodyNode body, TypeParameterListNode generics = null)
			: base(keyword, name, generics)
		{
			Body = body;
		}

		public static new InterfaceNode Parse(Source source)
		{
			int pos = source.Position;
			var keyword = source.ExpectIdentifier("interface");
			if (keyword is null)
				return null;

			var name = NameNode.Parse(source);
			if (name is null && source.Reset(pos))
				return null;
			var generics = TypeParameterListNode.Parse(source);
			var body = TypeBodyNode.Parse(source);
			if (body is null && source.Reset(pos))
				return null;

			return new InterfaceNode(keyword, name, body, generics);
		}
	}

	public sealed class StructNode : TypeNode
	{
		public readonly TypeBodyNode Body;

		public StructNode(IdentifierToken keyword, NameNode name, TypeBodyNode body, TypeParameterListNode generics = null)
			: base(keyword, name, generics)
		{
			Body = body;
		}

		public static new StructNode Parse(Source source)
		{
			int pos = source.Position;
			var keyword = source.ExpectIdentifier("struct");
			if (keyword is null)
				return null;

			var name = NameNode.Parse(source);
			if (name is null && source.Reset(pos))
				return null;
			var generics = TypeParameterListNode.Parse(source);
			var body = TypeBodyNode.Parse(source);
			if (body is null && source.Reset(pos))
				return null;

			return new StructNode(keyword, name, body, generics);
		}
	}

	public sealed class ClassNode : TypeNode
	{
		public readonly TypeBodyNode Body;

		public ClassNode(IdentifierToken keyword, NameNode name, TypeBodyNode body, TypeParameterListNode generics = null)
			: base(keyword, name, generics)
		{
			Body = body;
		}

		public static new ClassNode Parse(Source source)
		{
			int pos = source.Position;
			var keyword = source.ExpectIdentifier("class");
			if (keyword is null)
				return null;

			var name = NameNode.Parse(source);
			if (name is null && source.Reset(pos))
				return null;
			var generics = TypeParameterListNode.Parse(source);
			var body = TypeBodyNode.Parse(source);
			if (body is null && source.Reset(pos))
				return null;

			return new ClassNode(keyword, name, body, generics);
		}
	}

	public sealed class TypeParameterListNode
	{
		public readonly ControlToken OpeningBracket;
		public readonly ReadOnlyCollection<TypeParameterNode> Parameters;
		public readonly ReadOnlyCollection<ControlToken> Commas;
		public readonly ControlToken ClosingBracket;

		public TypeParameterListNode(ControlToken open, List<TypeParameterNode> parameters, List<ControlToken> commas, ControlToken close)
			=> (OpeningBracket, Parameters, Commas, ClosingBracket) = (open, parameters.AsReadOnly(), commas.AsReadOnly(), close);

		public static TypeParameterListNode Parse(Source source)
		{
			int pos = source.Position;
			var open = source.ExpectControl('<');
			if (open is null)
				return null;

			var elements = new List<TypeParameterNode>();
			var commas = new List<ControlToken>();
			var arg = TypeParameterNode.Parse(source);
			if (arg is null && source.Reset(pos))
				return null;
			elements.Add(arg);

			int subPos = source.Position;
			while (true)
			{
				var comma = source.ExpectControl(',');
				if (comma is null)
					break;

				arg = TypeParameterNode.Parse(source);
				if (arg is null)
					break;

				subPos = source.Position;
				commas.Add(comma);
				elements.Add(arg);
			}
			source.Reset(subPos);

			var close = source.ExpectControl('>');
			if (close is null && source.Reset(pos))
				return null;

			return new TypeParameterListNode(open, elements, commas, close);
		}
	}

	public sealed class TypeParameterNode
	{
		public readonly IdentifierToken Variance;
		public bool HasVariance => Variance != null;
		public readonly NameNode Name;

		public TypeParameterNode(NameNode name, IdentifierToken variance = null)
			=> (Variance, Name) = (variance, name);

		public static TypeParameterNode Parse(Source source)
		{
			int pos = source.Position;
			var variance =
				source.ExpectIdentifier("out") ??
				source.ExpectIdentifier("in");

			var name = NameNode.Parse(source);
			if (!(variance is null) && name is null)
			{
				source.Reset(pos);
				variance = null;
				name = NameNode.Parse(source);
			}
			if (name is null)
				return null;

			return new TypeParameterNode(name, variance);
		}
	}

	public sealed class TypeBodyNode
	{
		public readonly ControlToken OpeningBracket;
		public readonly ReadOnlyCollection<MemberNode> Members;
		public readonly ControlToken ClosingBracket;

		public TypeBodyNode(ControlToken open, List<MemberNode> members, ControlToken close)
		{
			OpeningBracket = open;
			Members = System.Array.AsReadOnly(members.ToArray());
			ClosingBracket = close;
		}

		public static TypeBodyNode Parse(Source source)
		{
			var open = source.ExpectControl('{');
			if (open is null)
				return null;

			int pos = source.Position;
			var members = new List<MemberNode>();
			while (true)
			{
				var close = source.ExpectControl('}');
				if (!(close is null))
					return new TypeBodyNode(open, members, close);

				var member = MemberNode.Parse(source);
				
				if (!(member is null))
					members.Add(member);
				//skip one token, if parsing failed.
				else if (!source.SkipToken())
					return null;
				pos = source.Position;
			}
		}
	}


	public sealed class FieldNode : MemberNode
	{
		public readonly ControlToken Colon;
		public readonly TypeReferenceNode Type;

		public FieldNode(NameNode name, ControlToken colon, TypeReferenceNode type)
			: base(name)
		{
			Colon = colon;
			Type = type;
		}

		public static new FieldNode Parse(Source source)
		{
			int pos = source.Position;
			var name = NameNode.Parse(source);
			if (name is null)
				return null;

			var colon = source.ExpectControl(':');
			if (colon is null)
			{
				source.Reset(pos);
				return null;
			}
			var type = TypeReferenceNode.Parse(source);
			if (type is null)
			{
				source.Reset(pos);
				return null;
			}

			return new FieldNode(name, colon, type);
		}
	}

	public sealed class PropertyNode : MemberNode
	{
		public readonly ControlToken OpeningBracket;
		public readonly PropertyAccessorNode Getter;
		public bool HasGetter => Getter != null;
		public readonly PropertyAccessorNode Setter;
		public bool HasSetter => Setter != null;
		public readonly ControlToken ClosingBracket;
		public readonly ControlToken Colon;
		public readonly TypeReferenceNode Type;

		public PropertyNode(NameNode name, ControlToken open, ControlToken close, ControlToken colon, TypeReferenceNode type, PropertyAccessorNode getter = null, PropertyAccessorNode setter = null)
			: base(name)
		{
			OpeningBracket = open;
			Getter = getter;
			Setter = setter;
			ClosingBracket = close;
			Colon = colon;
			Type = type;
		}

		public static new PropertyNode Parse(Source source)
		{
			int pos = source.Position;
			var name = NameNode.Parse(source);
			if (name is null)
				return null;

			var open = source.ExpectControl('{');
			if (open is null)
			{
				source.Reset(pos);
				return null;
			}

			var get = PropertyAccessorNode.ParseGet(source);
			var set = PropertyAccessorNode.ParseSet(source);
			if (get is null)
			{
				if (set is null)
				{
					//source.ParseError();
					source.Reset(pos);
					return null;
				}
				get = PropertyAccessorNode.ParseGet(source);
			}

			var close = source.ExpectControl('}');
			if (close is null)
			{
				source.Reset(pos);
				return null;
			}
			var colon = source.ExpectControl(':');
			if (colon is null)
			{
				source.Reset(pos);
				return null;
			}
			var type = TypeReferenceNode.Parse(source);
			if (type is null)
			{
				source.Reset(pos);
				return null;
			}

			return new PropertyNode(name, open, close, colon, type, get, set);
		}
	}

	public sealed class PropertyAccessorNode
	{
		public readonly IdentifierToken Keyword;
		public readonly AccessorReferenceNode Reference;
		public bool HasReference => Reference != null;
		public readonly ControlToken Semicolon;

		public PropertyAccessorNode(IdentifierToken keyword, ControlToken semicolon, AccessorReferenceNode reference = null)
		{
			Keyword = keyword;
			Reference = reference;
			Semicolon = semicolon;
		}

		public static PropertyAccessorNode ParseGet(Source source)
		{
			return Parse(source, "get");
		}

		public static PropertyAccessorNode ParseSet(Source source)
		{
			return Parse(source, "set");
		}

		private static PropertyAccessorNode Parse(Source source, string key)
		{
			int pos = source.Position;
			var keyword = source.ExpectIdentifier(key);
			if (keyword is null)
				return null;

			var reference = AccessorReferenceNode.Parse(source);

			var semicolon = source.ExpectControl(';');
			if (semicolon is null)
			{
				source.Reset(pos);
				return null;
			}

			return new PropertyAccessorNode(keyword, semicolon, reference);
		}
	}

	public sealed class AccessorReferenceNode
	{
		public readonly ControlToken Assignment;
		public readonly IdentifierToken Name;
		public readonly TupleTypeNode Parameters;
		public bool HasParameters => !(Parameters is null);

		public AccessorReferenceNode(ControlToken assignment, IdentifierToken name, TupleTypeNode parameterList = null)
		{
			Assignment = assignment;
			Name = name;
			Parameters = parameterList;
		}

		public static AccessorReferenceNode Parse(Source source)
		{
			int pos = source.Position;
			var assignment = source.ExpectControl('=');
			if (assignment is null)
				return null;

			var name = source.ExpectIdentifier();
			if (name is null)
			{
				source.Reset(pos);
				return null;
			}

			var parameters = TupleTypeNode.Parse(source);

			return new AccessorReferenceNode(assignment, name, parameters);
		}
	}

	public sealed class EventNode : MemberNode
	{
		public readonly ControlToken Tilde;
		public readonly TypeReferenceNode Type;

		public EventNode(NameNode name, ControlToken tilde, TypeReferenceNode type)
			: base(name)
		{
			Tilde = tilde;
			Type = type;
		}

		public static new EventNode Parse(Source source)
		{
			int pos = source.Position;
			var name = NameNode.Parse(source);
			if (name is null)
				return null;

			var colon = source.ExpectControl('~');
			if (colon is null)
			{
				source.Reset(pos);
				return null;
			}
			var type = TypeReferenceNode.Parse(source);
			if (type is null)
			{
				source.Reset(pos);
				return null;
			}

			return new EventNode(name, colon, type);
		}
	}

	public sealed class MethodNode : MemberNode
	{
		public readonly ParameterListNode ParameterList;
		public readonly ControlToken Colon;
		public readonly TypeReferenceNode Type;

		public MethodNode(NameNode name, ParameterListNode parameters, ControlToken colon, TypeReferenceNode type)
			: base(name)
		{
			ParameterList = parameters;
			Colon = colon;
			Type = type;
		}

		public static new MethodNode Parse(Source source)
		{
			int pos = source.Position;
			var name = NameNode.Parse(source);
			if (name is null)
				return null;

			var parameters = ParameterListNode.Parse(source);
			if (parameters is null)
			{
				source.Reset(pos);
				return null;
			}

			var colon = source.ExpectControl(':');
			if (colon is null)
			{
				source.Reset(pos);
				return null;
			}
			var type = TypeReferenceNode.Parse(source);
			if (type is null)
			{
				source.Reset(pos);
				return null;
			}

			return new MethodNode(name, parameters, colon, type);
		}
	}

	public sealed class ParameterListNode
	{
		public readonly ControlToken OpeningBracket;
		public readonly IdentifierToken ThisKeyword;
		public bool HasThisKeyword => ThisKeyword != null;
		public readonly ReadOnlyCollection<ParameterNode> Parameters;
		public readonly ReadOnlyCollection<ControlToken> Commas;
		public readonly ControlToken ClosingBracket;

		public ParameterListNode(ControlToken open, List<ParameterNode> parameters, List<ControlToken> commas, ControlToken close, IdentifierToken thisKeyword = null)
			=> (OpeningBracket, Parameters, Commas, ClosingBracket) = (open, parameters.AsReadOnly(), commas.AsReadOnly(), close);

		public static ParameterListNode Parse(Source source)
		{
			int pos = source.Position;
			var open = source.ExpectControl('(');
			if (open is null)
				return null;

			int subPos = source.Position;
			var thisKeyword = source.ExpectIdentifier("this");
			var param = ParameterNode.Parse(source);
			if (!(thisKeyword is null) && param is null)
			{
				source.Reset(subPos);
				thisKeyword = null;
				param = ParameterNode.Parse(source);
			}

			var parameters = new List<ParameterNode>();
			var commas = new List<ControlToken>();
			if (param is null)
			{
				var end = source.ExpectControl(')');
				if (end is null)
				{
					source.Reset(pos);
					return null;
				}
				return new ParameterListNode(open, parameters, commas, end, thisKeyword);
			}
			parameters.Add(param);

			subPos = source.Position;
			while (true)
			{
				var comma = source.ExpectControl(',');
				if (comma is null)
					break;

				param = ParameterNode.Parse(source);
				if (param is null)
					break;

				subPos = source.Position;
				commas.Add(comma);
				parameters.Add(param);
			}
			source.Reset(subPos);

			var close = source.ExpectControl(')');
			if (close is null && source.Reset(pos))
				return null;

			return new ParameterListNode(open, parameters, commas, close);
		}
	}

	public sealed class ParameterNode
	{
		public readonly NameNode Name;
		public readonly ControlToken Colon;
		public readonly ControlToken Optional;
		public bool IsOptional => Optional != null;
		public readonly IdentifierToken Policy;
		public bool HasPolicy => Policy != null;
		public readonly TypeReferenceNode Type;

		public ParameterNode(NameNode name, ControlToken colon, TypeReferenceNode type, IdentifierToken policy = null, ControlToken optional = null)
			=> (Name, Colon, Optional, Policy, Type) = (name, colon, optional, policy, type);

		public static ParameterNode Parse(Source source)
		{
			int pos = source.Position;
			var name = NameNode.Parse(source);
			if (name is null)
				return null;
			var colon = source.ExpectControl(':');
			if (colon is null)
			{
				source.Reset(pos);
				return null;
			}
			var optional = source.ExpectControl('?');

			int subPos = source.Position;
			var policy =
				source.ExpectIdentifier("ref") ??
				source.ExpectIdentifier("out") ??
				source.ExpectIdentifier("in");
			var type = TypeReferenceNode.Parse(source);
			if (!(policy is null) && type is null)
			{
				source.Reset(subPos);
				policy = null;
				type = TypeReferenceNode.Parse(source);
			}

			if (type is null)
			{
				source.Reset(pos);
				return null;
			}
			return new ParameterNode(name, colon, type, policy, optional);
		}
	}


	public abstract class TypeReferenceNode
	{
		public static TypeReferenceNode Parse(Source source)
		{
			TypeReferenceNode core = TupleTypeNode.Parse(source);
			if (core is null)
			{
				core = TypeNameNode.Parse(source);
				if (core is null)
					return null;
			}

			var question = source.ExpectControl('?');
			if (!(question is null))
				core = new NullableTypeNode(core, question);

			int pos = source.Position;
			var commas = new List<ControlToken>();
			while (true)
			{
				var open = source.ExpectControl('[');
				if (open is null)
					break;
				var comma = source.ExpectControl(',');
				while (!(comma is null))
				{
					commas.Add(comma);
					comma = source.ExpectControl(',');
				}
				var close = source.ExpectControl(']');
				if (close is null)
					break;
				pos = source.Position;
				core = new ArrayTypeNode(core, open, commas, close);
			}
			source.Reset(pos);

			return core;
		}
	}

	public sealed class QualifiedTypeNameNode : TypeReferenceNode
	{
		public readonly TypeReferenceNode Left;
		public readonly ControlToken Dot;
		public readonly TypeNameNode Right;

		internal QualifiedTypeNameNode(TypeReferenceNode left, ControlToken dot, TypeNameNode right)
			=> (Left, Dot, Right) = (left, dot, right);

		public static new TypeReferenceNode Parse(Source source)
		{
			TypeReferenceNode left = TypeNameNode.Parse(source);
			if (left is null)
				return null;

			int pos = source.Position;
			while (true)
			{
				var dot = source.ExpectControl('.');
				if (dot is null)
					break;

				var right = TypeNameNode.Parse(source);
				if (right is null)
					break;

				pos = source.Position;
				left = new QualifiedTypeNameNode(left, dot, right);
			}
			source.Reset(pos);

			return left;
		}
	}

	public sealed class TypeNameNode : TypeReferenceNode
	{
		public readonly IdentifierToken Typename;
		public readonly TypeArgumentListNode Generics;
		public bool HasGenerics => !(Generics is null);

		private TypeNameNode(IdentifierToken typename, TypeArgumentListNode generics = null)
			=> (Typename, Generics) = (typename, generics);

		public static new TypeNameNode Parse(Source source)
		{
			var typename = source.ExpectIdentifier();
			if (typename is null)
				return null;

			var generics = TypeArgumentListNode.Parse(source);

			return new TypeNameNode(typename, generics);
		}
	}

	public sealed class TypeArgumentListNode
	{
		public readonly ControlToken OpeningBracket;
		public readonly ReadOnlyCollection<TypeReferenceNode> Arguments;
		public readonly ReadOnlyCollection<ControlToken> Commas;
		public readonly ControlToken ClosingBracket;

		private TypeArgumentListNode(ControlToken open, List<TypeReferenceNode> arguments, List<ControlToken> commas, ControlToken close)
			=> (OpeningBracket, Arguments, Commas, ClosingBracket) = (open, arguments.AsReadOnly(), commas.AsReadOnly(), close);

		public static TypeArgumentListNode Parse(Source source)
		{
			int pos = source.Position;
			var open = source.ExpectControl('<');
			if (open is null)
				return null;

			var arguments = new List<TypeReferenceNode>();
			var commas = new List<ControlToken>();
			var arg = TypeReferenceNode.Parse(source);
			if (arg is null && source.Reset(pos))
				return null;
			arguments.Add(arg);

			int subPos = source.Position;
			while (true)
			{
				var comma = source.ExpectControl(',');
				if (comma is null)
					break;

				arg = TypeReferenceNode.Parse(source);
				if (arg is null)
					break;

				subPos = source.Position;
				commas.Add(comma);
				arguments.Add(arg);
			}
			source.Reset(subPos);

			var close = source.ExpectControl('>');
			if (close is null && source.Reset(pos))
				return null;

			return new TypeArgumentListNode(open, arguments, commas, close);
		}
	}

	public sealed class ArrayTypeNode : TypeReferenceNode
	{
		public readonly TypeReferenceNode Enclosed;
		public readonly ControlToken OpeningBracket;
		public readonly ReadOnlyCollection<ControlToken> Commas;
		public readonly ControlToken ClosingBracket;

		public ArrayTypeNode(TypeReferenceNode enclosed, ControlToken open, List<ControlToken> commas, ControlToken close)
			=> (Enclosed, OpeningBracket, Commas, ClosingBracket) = (enclosed, open, commas.AsReadOnly(), close);
	}

	public sealed class NullableTypeNode : TypeReferenceNode
	{
		public readonly TypeReferenceNode Enclosed;
		public readonly ControlToken Questionmark;

		public NullableTypeNode(TypeReferenceNode enclosed, ControlToken questionmark)
			=> (Enclosed, Questionmark) = (enclosed, questionmark);
	}

	public sealed class TupleTypeNode : TypeReferenceNode
	{
		public readonly ControlToken OpeningBracket;
		public readonly ReadOnlyCollection<TypeReferenceNode> Elements;
		public readonly ReadOnlyCollection<ControlToken> Commas;
		public readonly ControlToken ClosingBracket;

		private TupleTypeNode(ControlToken open, List<TypeReferenceNode> elements, List<ControlToken> commas, ControlToken close)
			=> (OpeningBracket, Elements, Commas, ClosingBracket) = (open, elements.AsReadOnly(), commas.AsReadOnly(), close);

		public static new TupleTypeNode Parse(Source source)
		{
			int pos = source.Position;
			var open = source.ExpectControl('(');
			if (open is null)
				return null;
			var arg = TypeReferenceNode.Parse(source);
			if (arg is null && source.Reset(pos))
				return null;

			var elements = new List<TypeReferenceNode>();
			var commas = new List<ControlToken>();
			elements.Add(arg);

			int subPos = source.Position;
			while (true)
			{
				var comma = source.ExpectControl(',');
				if (comma is null)
					break;

				arg = TypeReferenceNode.Parse(source);
				if (arg is null)
					break;

				subPos = source.Position;
				commas.Add(comma);
				elements.Add(arg);
			}
			source.Reset(subPos);

			var close = source.ExpectControl(')');
			if (close is null && source.Reset(pos))
				return null;

			return new TupleTypeNode(open, elements, commas, close);
		}
	}

	public sealed class NameNode
	{
		public readonly IdentifierToken Name;
		public readonly NameChangeNode Rename;
		public bool HasRename => !(Rename is null);

		public NameNode(IdentifierToken name, NameChangeNode rename = null)
			=> (Name, Rename) = (name, rename);

		public static NameNode Parse(Source source)
		{
			int pos = source.Position;
			var name = source.ExpectIdentifier();
			if (name is null)
				return null;

			var change = NameChangeNode.Parse(source);

			return new NameNode(name, change);
		}
	}

	public sealed class NameChangeNode
	{
		public readonly ControlToken Assignment;
		public readonly IdentifierToken NewName;

		public NameChangeNode(ControlToken assignment, IdentifierToken newName)
			=> (Assignment, NewName) = (assignment, newName);

		public static NameChangeNode Parse(Source source)
		{
			int pos = source.Position;
			var equals = source.ExpectControl('=');
			if (equals is null)
				return null;

			var value = source.ExpectIdentifier();
			if (value is null && source.Reset(pos))
				return null;

			return new NameChangeNode(equals, value);
		}
	}
}
