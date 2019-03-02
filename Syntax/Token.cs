using System.Text;

namespace ILPatcher.Syntax
{
	public abstract class Token : SyntaxUnit
	{
		public readonly TokenType Type;

		public abstract string Identifier { get; }

		internal Token(TokenType type, Span location)
			: base(location)
			=> Type = type;


		public override string ToString()
		{
			var text = new StringBuilder();
			//text.Append('[');
			text.Append(Type);
			text.Append(": \"");
			text.Append(Identifier);
			text.Append("\" @");
			text.Append(Span);
			//text.Append(']');
			return text.ToString();
		}
	}
}
