using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ILPatcher.Syntax
{
	public sealed class SyntaxTree
	{
		public readonly NamespaceNode Namespace;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasNamespace => !(Namespace is null);
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly ReadOnlyCollection<TypeNode> Types;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly EndOfFileToken End;
		public readonly bool HasErrors;

		public SyntaxTree(List<TypeNode> types, NamespaceNode @namespace = null)
		{
			Namespace = @namespace;
			End = new EndOfFileToken(new Span());
			if (types is null)
				throw new ArgumentNullException(nameof(types));
			if (types.Any(m => m is null))
				throw new ArgumentException("Types may not contain null", nameof(types));
			Types = Array.AsReadOnly(types.ToArray());
		}

		private SyntaxTree(List<TypeNode> types, EndOfFileToken end, NamespaceNode @namespace, bool errors)
		{
			Namespace = @namespace;
			End = end;
			Types = Array.AsReadOnly(types.ToArray());
			HasErrors = errors;
		}


		public static SyntaxTree Parse(Source source)
		{
			var @namespace = NamespaceNode.Parse(source);

			var types = new List<TypeNode>();
			while (true)
			{
				var end = source.ExpectEnd();
				if (!(end is null))
					return new SyntaxTree(types, end, @namespace, source.HasErrors);

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

	public abstract class SyntaxNode : SyntaxUnit
	{
		public readonly Span FullSpan;

		internal SyntaxNode(Span span, Span fullSpan)
			: base(span)
		{
			FullSpan = fullSpan;
		}
	}

	public sealed class NamespaceNode
	{
		public readonly IdentifierToken Keyword;
		public readonly TypeReferenceNode Namespaces;

		public NamespaceNode(TypeReferenceNode fullNamespace)
		{
			Keyword = new IdentifierToken("namespace", new Span());
			Namespaces = fullNamespace ?? throw new ArgumentNullException(nameof(fullNamespace));
		}

		private NamespaceNode(IdentifierToken keyword, TypeReferenceNode fullNamespace)
		{
			Keyword = keyword;
			Namespaces = fullNamespace;
		}


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

		public StringBuilder WriteTo(StringBuilder text)
		{
			text.Append("namespace ");
			return Namespaces.WriteTo(text);
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}

	public abstract class MemberNode
	{
		public readonly NameNode Name;

		internal MemberNode(NameNode name)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}


		public static MemberNode Parse(Source source)
		{
			return
				FieldNode.Parse(source) ??
				PropertyNode.Parse(source) ??
				EventNode.Parse(source) ??
				MethodNode.Parse(source) ??
				(MemberNode) TypeNode.Parse(source);
		}


		public abstract StringBuilder WriteTo(StringBuilder text);

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}

	public abstract class TypeNode : MemberNode
	{
		public readonly IdentifierToken Keyword;
		public readonly TypeParameterListNode Generics;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasGenerics => Generics != null;

		internal TypeNode(IdentifierToken keyword, NameNode name, TypeParameterListNode generics = null)
			: base(name)
		{
			Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
			Generics = generics;
		}


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
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken OpeningBracket;
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly ReadOnlyCollection<NameNode> Constants;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ReadOnlyCollection<ControlToken> Commas;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken ClosingBracket;

		public EnumNode(NameNode name, List<NameNode> constants)
			: base(new IdentifierToken("enum", new Span()), name)
		{
			OpeningBracket = new ControlToken('{', new Span());
			ClosingBracket = new ControlToken('}', new Span());
			if (constants is null)
				throw new ArgumentNullException(nameof(constants));
			if (constants.Any(m => m is null))
				throw new ArgumentException("Constants may not contain null", nameof(constants));
			Constants = Array.AsReadOnly(constants.ToArray());
			int commaCount = Constants.Count - 1;
			var commas = new ControlToken[commaCount];
			var comma = new ControlToken(',', new Span());
			for (int i = 0; i < commaCount; i++)
			{
				commas[i] = comma;
			}
			Commas = Array.AsReadOnly(commas);
		}

		private EnumNode(IdentifierToken keyword, NameNode name,
			ControlToken open, List<NameNode> constants,
			List<ControlToken> commas, ControlToken close)
			: base(keyword, name)
		{
			OpeningBracket = open;
			ClosingBracket = close;
			Constants = Array.AsReadOnly(constants.ToArray());
			Commas = Array.AsReadOnly(commas.ToArray());
		}


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

		public override StringBuilder WriteTo(StringBuilder text)
		{
			text.Append("enum ");
			Name.WriteTo(text);
			text.Append('{');
			if (Constants.Count > 0)
				text.Append("...");
			return text.Append('}');
		}
	}

	public sealed class DelegateNode : TypeNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly ParameterListNode Parameters;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken Colon;
		public readonly TypeReferenceNode ReturnType;

		public DelegateNode(NameNode name, ParameterListNode parameters, TypeReferenceNode returnType, TypeParameterListNode generics = null)
			: base(new IdentifierToken("delegate", new Span()), name, generics)
		{
			Colon = new ControlToken(':', new Span());
			Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
			ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
		}

		private DelegateNode(IdentifierToken keyword, NameNode name, ParameterListNode parameters, ControlToken colon, TypeReferenceNode returnType, TypeParameterListNode generics = null)
			: base(keyword, name, generics)
		{
			Parameters = parameters;
			Colon = colon;
			ReturnType = returnType;
		}


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

		public override StringBuilder WriteTo(StringBuilder text)
		{
			text.Append("delegate ");
			Name.WriteTo(text);
			Generics?.WriteTo(text);
			Parameters.WriteTo(text);
			text.Append(" : ");
			return ReturnType.WriteTo(text);
		}
	}

	public sealed class InterfaceNode : TypeNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly TypeBodyNode Body;

		public InterfaceNode(NameNode name, TypeBodyNode body, TypeParameterListNode generics = null)
			: base(new IdentifierToken("interface", new Span()), name, generics)
		{
			Body = body ?? throw new ArgumentNullException(nameof(body));
		}

		private InterfaceNode(IdentifierToken keyword, NameNode name, TypeBodyNode body, TypeParameterListNode generics = null)
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

		public override StringBuilder WriteTo(StringBuilder text)
		{
			text.Append("interface ");
			Name.WriteTo(text);
			Generics?.WriteTo(text);
			return Body.WriteTo(text);
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}

	public sealed class StructNode : TypeNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly TypeBodyNode Body;

		public StructNode(NameNode name, TypeBodyNode body, TypeParameterListNode generics = null)
			: base(new IdentifierToken("struct", new Span()), name, generics)
		{
			Body = body ?? throw new ArgumentNullException(nameof(body));
		}

		private StructNode(IdentifierToken keyword, NameNode name, TypeBodyNode body, TypeParameterListNode generics = null)
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

		public override StringBuilder WriteTo(StringBuilder text)
		{
			text.Append("struct ");
			Name.WriteTo(text);
			Generics?.WriteTo(text);
			return Body.WriteTo(text);
		}
	}

	public sealed class ClassNode : TypeNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly TypeBodyNode Body;

		public ClassNode(NameNode name, TypeBodyNode body, TypeParameterListNode generics = null)
			: base(new IdentifierToken("class", new Span()), name, generics)
		{
			Body = body ?? throw new ArgumentNullException(nameof(body));
		}

		private ClassNode(IdentifierToken keyword, NameNode name, TypeBodyNode body, TypeParameterListNode generics = null)
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

		public override StringBuilder WriteTo(StringBuilder text)
		{
			text.Append("class ");
			Name.WriteTo(text);
			Generics?.WriteTo(text);
			return Body.WriteTo(text);
		}
	}

	public sealed class TypeParameterListNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken OpeningBracket;
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly ReadOnlyCollection<TypeParameterNode> Parameters;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ReadOnlyCollection<ControlToken> Commas;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken ClosingBracket;

		public TypeParameterListNode(List<TypeParameterNode> parameters)
		{
			OpeningBracket = new ControlToken('<', new Span());
			ClosingBracket = new ControlToken('>', new Span());
			if (parameters is null)
				throw new ArgumentNullException(nameof(parameters));
			if (parameters.Any(m => m is null))
				throw new ArgumentException("Parameters may not contain null", nameof(parameters));
			Parameters = Array.AsReadOnly(parameters.ToArray());
			int commaCount = Parameters.Count - 1;
			var commas = new ControlToken[commaCount];
			var comma = new ControlToken(',', new Span());
			for (int i = 0; i < commaCount; i++)
			{
				commas[i] = comma;
			}
			Commas = Array.AsReadOnly(commas);
		}

		private TypeParameterListNode(ControlToken open, List<TypeParameterNode> parameters, List<ControlToken> commas, ControlToken close)
		{
			OpeningBracket = open;
			ClosingBracket = close;
			Parameters = Array.AsReadOnly(parameters.ToArray());
			Commas = Array.AsReadOnly(commas.ToArray());
		}


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

		public StringBuilder WriteTo(StringBuilder text)
		{
			text.Append('<');
			for (int i = 0; i < Parameters.Count; i++)
			{
				if (i > 0)
					text.Append(",");
				Parameters[i].WriteTo(text);
			}
			return text.Append('>');
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}

	public sealed class TypeParameterNode
	{
		public readonly IdentifierToken Variance;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasVariance => Variance != null;
		public readonly NameNode Name;

		public TypeParameterNode(NameNode name, Model.GenericVariance variance = Model.GenericVariance.Invariant)
		{
			if (variance == Model.GenericVariance.Covariant)
				Variance = new IdentifierToken("out", new Span());
			else if (variance == Model.GenericVariance.Contravariant)
				Variance = new IdentifierToken("in", new Span());
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		private TypeParameterNode(NameNode name, IdentifierToken variance = null)
		{
			Variance = variance;
			Name = name;
		}


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

		public StringBuilder WriteTo(StringBuilder text)
		{
			if (HasVariance)
			{
				text.Append(Variance.Name);
				text.Append(' ');
			}
			return Name.WriteTo(text);
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}

	public sealed class TypeBodyNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken OpeningBracket;
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly ReadOnlyCollection<MemberNode> Members;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken ClosingBracket;

		public TypeBodyNode(List<MemberNode> members)
		{
			OpeningBracket = new ControlToken('{', new Span());
			ClosingBracket = new ControlToken('}', new Span());
			if (members is null)
				throw new ArgumentNullException(nameof(members));
			if (members.Any(m => m is null))
				throw new ArgumentException("Members may not contain null", nameof(members));
			Members = Array.AsReadOnly(members.ToArray());
		}

		private TypeBodyNode(ControlToken open, List<MemberNode> members, ControlToken close)
		{
			OpeningBracket = open;
			ClosingBracket = close;
			Members = Array.AsReadOnly(members.ToArray());
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

		public StringBuilder WriteTo(StringBuilder text)
		{
			text.Append('{');
			if (Members.Count > 0)
			{
				text.Append("...");
			}
			text.Append('}');
			return text;
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}


	public sealed class FieldNode : MemberNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken Colon;
		public readonly TypeReferenceNode Type;

		public FieldNode(NameNode name, TypeReferenceNode type)
			: base(name)
		{
			Colon = new ControlToken(':', new Span());
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		private FieldNode(NameNode name, ControlToken colon, TypeReferenceNode type)
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

		public override StringBuilder WriteTo(StringBuilder text)
		{
			Name.WriteTo(text);
			text.Append(" : ");
			return Type.WriteTo(text);
		}
	}

	public sealed class PropertyNode : MemberNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken OpeningBracket;
		public readonly PropertyAccessorNode Getter;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasGetter => Getter != null;
		public readonly PropertyAccessorNode Setter;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasSetter => Setter != null;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken ClosingBracket;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken Colon;
		public readonly TypeReferenceNode Type;

		public PropertyNode(NameNode name, TypeReferenceNode type,
			PropertyAccessorNode getter = null, PropertyAccessorNode setter = null)
			: base(name)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
			OpeningBracket = new ControlToken('{', new Span());
			ClosingBracket = new ControlToken('}', new Span());
			Colon = new ControlToken(':', new Span());
			Getter = getter;
			Setter = setter;
		}

		private PropertyNode(NameNode name, ControlToken open,
			ControlToken close, ControlToken colon, TypeReferenceNode type,
			PropertyAccessorNode getter = null, PropertyAccessorNode setter = null)
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

		public override StringBuilder WriteTo(StringBuilder text)
		{
			Name.WriteTo(text);
			text.Append(" { ");
			Getter?.WriteTo(text);
			if (HasGetter && HasSetter)
				text.Append(' ');
			Setter?.WriteTo(text);
			text.Append(" } : ");
			return Type.WriteTo(text);
		}
	}

	public sealed class PropertyAccessorNode
	{
		public readonly IdentifierToken Keyword;
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly AccessorReferenceNode Reference;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasReference => Reference != null;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken Semicolon;

		public PropertyAccessorNode(bool isGetter, AccessorReferenceNode reference = null)
		{
			if (isGetter)
				Keyword = new IdentifierToken("get", new Span());
			else
				Keyword = new IdentifierToken("set", new Span());
			Reference = reference;
			Semicolon = new ControlToken(';', new Span());
		}

		private PropertyAccessorNode(IdentifierToken keyword, ControlToken semicolon, AccessorReferenceNode reference = null)
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

		public StringBuilder WriteTo(StringBuilder text)
		{
			text.Append(Keyword.Name);
			Reference?.WriteTo(text);
			return text.Append(';');
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}

	public sealed class AccessorReferenceNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken Assignment;
		public readonly IdentifierToken Name;
		public readonly TupleTypeNode Parameters;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasParameters => !(Parameters is null);

		public AccessorReferenceNode(string name, TupleTypeNode parameterList = null)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			Name = new IdentifierToken(name, new Span());
			Assignment = new ControlToken('=', new Span());
			Parameters = parameterList;
		}

		private AccessorReferenceNode(ControlToken assignment, IdentifierToken name, TupleTypeNode parameterList = null)
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

		public StringBuilder WriteTo(StringBuilder text)
		{
			text.Append('=');
			text.Append(Name.Name);
			if (HasParameters)
				Parameters.WriteTo(text);
			return text;
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}

	public sealed class EventNode : MemberNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken Tilde;
		public readonly TypeReferenceNode Type;

		public EventNode(NameNode name, TypeReferenceNode type)
			: base(name)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Tilde = new ControlToken('~', new Span());
		}

		private EventNode(NameNode name, ControlToken tilde, TypeReferenceNode type)
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

		public override StringBuilder WriteTo(StringBuilder text)
		{
			Name.WriteTo(text);
			text.Append(" ~ ");
			return Type.WriteTo(text);
		}
	}

	public sealed class MethodNode : MemberNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly ParameterListNode ParameterList;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken Colon;
		public readonly TypeReferenceNode Type;

		public MethodNode(NameNode name, ParameterListNode parameters, TypeReferenceNode type)
			: base(name)
		{
			ParameterList = parameters ?? throw new ArgumentNullException(nameof(parameters));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Colon = new ControlToken(':', new Span());
		}

		private MethodNode(NameNode name, ParameterListNode parameters, ControlToken colon, TypeReferenceNode type)
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

		public override StringBuilder WriteTo(StringBuilder text)
		{
			Name.WriteTo(text);
			text.Append(' ');
			ParameterList.WriteTo(text);
			text.Append(" : ");
			return Type.WriteTo(text);
		}
	}

	public sealed class ParameterListNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken OpeningBracket;
		public readonly IdentifierToken ThisKeyword;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasThisKeyword => ThisKeyword != null;
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly ReadOnlyCollection<ParameterNode> Parameters;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ReadOnlyCollection<ControlToken> Commas;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken ClosingBracket;

		public ParameterListNode(List<ParameterNode> parameters, bool isExtension = false)
		{
			OpeningBracket = new ControlToken('(', new Span());
			ClosingBracket = new ControlToken(')', new Span());
			if (isExtension)
				ThisKeyword = new IdentifierToken("this", new Span());

			if (parameters is null)
				throw new ArgumentNullException(nameof(parameters));
			if (parameters.Any(m => m is null))
				throw new ArgumentException("Parameters may not contain null", nameof(parameters));
			Parameters = Array.AsReadOnly(parameters.ToArray());

			int commaCount = Parameters.Count - 1;
			var commas = new ControlToken[commaCount];
			var comma = new ControlToken(',', new Span());
			for (int i = 0; i < commaCount; i++)
			{
				commas[i] = comma;
			}
			Commas = Array.AsReadOnly(commas);
		}

		private ParameterListNode(ControlToken open, IdentifierToken thisKeyword, List<ParameterNode> parameters, List<ControlToken> commas, ControlToken close)
		{
			OpeningBracket = open;
			ThisKeyword = thisKeyword;
			Parameters = Array.AsReadOnly(parameters.ToArray());
			Commas = Array.AsReadOnly(commas.ToArray());
			ClosingBracket = close;
		}


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
				return new ParameterListNode(open, thisKeyword, parameters, commas, end);
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

			return new ParameterListNode(open, thisKeyword, parameters, commas, close);
		}

		public StringBuilder WriteTo(StringBuilder text)
		{
			text.Append('(');
			if (HasThisKeyword)
				text.Append("this ");
			for (int i = 0; i < Parameters.Count; i++)
			{
				if (i > 0)
					text.Append(", ");
				Parameters[i].WriteTo(text);
			}
			return text.Append(')');
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}

	public sealed class ParameterNode
	{
		public readonly NameNode Name;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken Colon;
		public readonly ControlToken Optional;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsOptional => Optional != null;
		public readonly IdentifierToken Policy;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasPolicy => Policy != null;
		public readonly TypeReferenceNode Type;

		public ParameterNode(NameNode name, TypeReferenceNode type,
			Model.ParameterPolicy policy = Model.ParameterPolicy.Value, bool isOptional = false)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Colon = new ControlToken(':', new Span());

			if (isOptional)
				Optional = new ControlToken('?', new Span());

			if (policy == Model.ParameterPolicy.Reference)
				Policy = new IdentifierToken("ref", new Span());
			else if (policy == Model.ParameterPolicy.In)
				Policy = new IdentifierToken("in", new Span());
			else if (policy == Model.ParameterPolicy.Out)
				Policy = new IdentifierToken("out", new Span());
			else if (policy == Model.ParameterPolicy.Params)
				Policy = new IdentifierToken("params", new Span());
		}

		private ParameterNode(NameNode name, ControlToken colon,
			TypeReferenceNode type, IdentifierToken policy = null, ControlToken optional = null)
		{
			Name = name;
			Colon = colon;
			Optional = optional;
			Policy = policy;
			Type = type;
		}


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

		public StringBuilder WriteTo(StringBuilder text)
		{
			Name.WriteTo(text);
			text.Append(" :");
			if (IsOptional)
				text.Append('?');
			text.Append(' ');
			if (HasPolicy)
				text.Append(Policy.Name);
			return Type.WriteTo(text);
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}


	public abstract class TypeReferenceNode
	{
		internal TypeReferenceNode() { }

		public static TypeReferenceNode Parse(Source source)
		{
			TypeReferenceNode core = TupleTypeNode.Parse(source);
			if (core is null)
			{
				core = QualifiedTypeNameNode.Parse(source);
				if (core is null)
					return null;
			}

			core = NullableTypeNode.Parse(source, core) ?? core;

			var array = ArrayTypeNode.Parse(source, core);
			while (!(array is null))
			{
				core = array;
				array = ArrayTypeNode.Parse(source, core);
			}

			return core;
		}

		public abstract StringBuilder WriteTo(StringBuilder text);

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}

	public sealed class QualifiedTypeNameNode : TypeReferenceNode
	{
		public readonly TypeReferenceNode Left;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken Dot;
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly TypeNameNode Right;

		public QualifiedTypeNameNode(TypeReferenceNode left, TypeNameNode right)
		{
			Left = left ?? throw new ArgumentNullException(nameof(left));
			Right = right ?? throw new ArgumentNullException(nameof(right));
			Dot = new ControlToken('.', new Span());
		}

		private QualifiedTypeNameNode(TypeReferenceNode left, ControlToken dot, TypeNameNode right)
		{
			Left = left;
			Dot = dot;
			Right = right;
		}


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

		public override StringBuilder WriteTo(StringBuilder text)
		{
			Left.WriteTo(text);
			text.Append('.');
			return Right.WriteTo(text);
		}
	}

	public sealed class TypeNameNode : TypeReferenceNode
	{
		public readonly IdentifierToken Typename;
		public readonly TypeArgumentListNode Generics;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasGenerics => !(Generics is null);

		public TypeNameNode(string typename, TypeArgumentListNode generics = null)
		{
			if (typename is null)
				throw new ArgumentNullException(nameof(typename));
			Typename = new IdentifierToken(typename, new Span());
			Generics = generics;
		}

		private TypeNameNode(IdentifierToken typename, TypeArgumentListNode generics = null)
		{
			Typename = typename;
			Generics = generics;
		}


		public static new TypeNameNode Parse(Source source)
		{
			var typename = source.ExpectIdentifier();
			if (typename is null)
				return null;

			var generics = TypeArgumentListNode.Parse(source);

			return new TypeNameNode(typename, generics);
		}

		public override StringBuilder WriteTo(StringBuilder text)
		{
			text.Append(Typename.Name);
			if (HasGenerics)
				Generics.WriteTo(text);
			return text;
		}
	}

	public sealed class TypeArgumentListNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken OpeningBracket;
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly ReadOnlyCollection<TypeReferenceNode> Arguments;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ReadOnlyCollection<ControlToken> Commas;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken ClosingBracket;

		public TypeArgumentListNode(List<TypeReferenceNode> arguments)
		{
			OpeningBracket = new ControlToken('<', new Span());
			ClosingBracket = new ControlToken('>', new Span());

			if (arguments is null)
				throw new ArgumentNullException(nameof(arguments));
			if (arguments.Any(m => m is null))
				throw new ArgumentException("Arguments may not contain null", nameof(arguments));
			Arguments = Array.AsReadOnly(arguments.ToArray());

			int commaCount = Arguments.Count - 1;
			var commas = new ControlToken[commaCount];
			var comma = new ControlToken(',', new Span());
			for (int i = 0; i < commaCount; i++)
			{
				commas[i] = comma;
			}
			Commas = Array.AsReadOnly(commas);
		}

		private TypeArgumentListNode(ControlToken open, List<TypeReferenceNode> arguments, List<ControlToken> commas, ControlToken close)
		{
			OpeningBracket = open;
			Arguments = Array.AsReadOnly(arguments.ToArray());
			Commas = Array.AsReadOnly(commas.ToArray());
			ClosingBracket = close;
		}


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

		public StringBuilder WriteTo(StringBuilder text)
		{
			text.Append('<');
			for (int i = 0; i < Arguments.Count; i++)
			{
				if (i > 0)
					text.Append(",");
				Arguments[i].WriteTo(text);
			}
			return text.Append('>');
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}
	
	public sealed class ArrayTypeNode : TypeReferenceNode
	{
		public readonly TypeReferenceNode Enclosed;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken OpeningBracket;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ReadOnlyCollection<ControlToken> Commas;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken ClosingBracket;

		public ArrayTypeNode(TypeReferenceNode enclosed, uint rank)
		{
			if (rank == 0)
				throw new ArgumentException("Array rank must be >= 1");

			OpeningBracket = new ControlToken('[', new Span());
			ClosingBracket = new ControlToken(']', new Span());
			Enclosed = enclosed ?? throw new ArgumentNullException(nameof(enclosed));

			rank--;
			var commas = new ControlToken[rank];
			var comma = new ControlToken(',', new Span());
			for (int i = 0; i < rank; i++)
			{
				commas[i] = comma;
			}
			Commas = Array.AsReadOnly(commas);
		}

		private ArrayTypeNode(TypeReferenceNode enclosed, ControlToken open, List<ControlToken> commas, ControlToken close)
		{
			Enclosed = enclosed;
			OpeningBracket = open;
			Commas = Array.AsReadOnly(commas.ToArray());
			ClosingBracket = close;
		}


		public static ArrayTypeNode Parse(Source source, TypeReferenceNode type)
		{
			int pos = source.Position;
			var open = source.ExpectControl('[');
			if (open is null)
				return null;

			var commas = new List<ControlToken>();
			var comma = source.ExpectControl(',');
			while (!(comma is null))
			{
				commas.Add(comma);
				comma = source.ExpectControl(',');
			}

			var close = source.ExpectControl(']');
			if (close is null)
			{
				source.Reset(pos);
				return null;
			}

			return new ArrayTypeNode(type, open, commas, close);
		}

		public override StringBuilder WriteTo(StringBuilder text)
		{
			Enclosed.WriteTo(text);
			text.Append('[');
			text.Append(',', Commas.Count);
			return text.Append(']');
		}
	}

	public sealed class NullableTypeNode : TypeReferenceNode
	{
		public readonly TypeReferenceNode Enclosed;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken Questionmark;

		public NullableTypeNode(TypeReferenceNode enclosed)
		{
			Enclosed = enclosed ?? throw new ArgumentNullException(nameof(enclosed));
			Questionmark = new ControlToken('?', new Span());
		}

		private NullableTypeNode(TypeReferenceNode enclosed, ControlToken questionmark)
		{
			Enclosed = enclosed;
			Questionmark = questionmark;
		}


		public static NullableTypeNode Parse(Source source, TypeReferenceNode type)
		{
			var question = source.ExpectControl('?');
			if (question is null)
				return null;

			return new NullableTypeNode(type, question);
		}

		public override StringBuilder WriteTo(StringBuilder text)
		{
			Enclosed.WriteTo(text);
			return text.Append('?');
		}
	}

	public sealed class TupleTypeNode : TypeReferenceNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken OpeningBracket;
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly ReadOnlyCollection<TypeReferenceNode> Elements;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ReadOnlyCollection<ControlToken> Commas;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken ClosingBracket;

		public TupleTypeNode(List<TypeReferenceNode> elements)
		{
			OpeningBracket = new ControlToken('(', new Span());
			ClosingBracket = new ControlToken(')', new Span());

			if (elements is null)
				throw new ArgumentNullException(nameof(elements));
			if (elements.Any(m => m is null))
				throw new ArgumentException("Elements may not contain null", nameof(elements));
			Elements = Array.AsReadOnly(elements.ToArray());

			int commaCount = Elements.Count - 1;
			var commas = new ControlToken[commaCount];
			var comma = new ControlToken(',', new Span());
			for (int i = 0; i < commaCount; i++)
			{
				commas[i] = comma;
			}
			Commas = Array.AsReadOnly(commas);
		}

		private TupleTypeNode(ControlToken open, List<TypeReferenceNode> elements, List<ControlToken> commas, ControlToken close)
		{
			OpeningBracket = open;
			Elements = Array.AsReadOnly(elements.ToArray());
			Commas = Array.AsReadOnly(commas.ToArray());
			ClosingBracket = close;
		}


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

		public override StringBuilder WriteTo(StringBuilder text)
		{
			text.Append('(');
			for (int i = 0; i < Elements.Count; i++)
			{
				if (i > 0)
					text.Append(", ");
				Elements[i].WriteTo(text);
			}
			return text.Append(')');
		}
	}

	public sealed class NameNode
	{
		public readonly IdentifierToken Name;
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public readonly NameChangeNode Rename;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasRename => !(Rename is null);

		public NameNode(string name, NameChangeNode rename = null)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			Name = new IdentifierToken(name, new Span());
			Rename = rename;
		}

		private NameNode(IdentifierToken name, NameChangeNode rename = null)
		{
			Name = name;
			Rename = rename;
		}


		public static NameNode Parse(Source source)
		{
			int pos = source.Position;
			var name = source.ExpectIdentifier();
			if (name is null)
				return null;

			var change = NameChangeNode.Parse(source);

			return new NameNode(name, change);
		}

		public StringBuilder WriteTo(StringBuilder text)
		{
			text.Append(Name.Name);
			if (HasRename)
				Rename.WriteTo(text);
			return text;
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}

	public sealed class NameChangeNode
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly ControlToken Assignment;
		public readonly IdentifierToken NewName;

		public NameChangeNode(string newName)
		{
			if (newName is null)
				throw new ArgumentNullException(nameof(newName));
			NewName = new IdentifierToken(newName, new Span());
			Assignment = new ControlToken('=', new Span());
		}

		private NameChangeNode(ControlToken equals, IdentifierToken newName)
		{
			Assignment = equals;
			NewName = newName;
		}


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

		public StringBuilder WriteTo(StringBuilder text)
		{
			text.Append('=');
			return text.Append(NewName.Name);
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}
	}
}
