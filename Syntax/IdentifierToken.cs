using System.Collections.Generic;
using System.Text;

namespace ILPatcher.Syntax
{
	public class IdentifierToken : SymbolToken
	{
		public readonly string Name;

		public override string Identifier => Name;

		public IdentifierToken(string name, Span location)
			: base(TokenType.Name, location)
		{
			Name = name;
		}

		private IdentifierToken(string name, Span location,
			List<TriviaToken> leading, List<TriviaToken> trailing)
			: base(TokenType.Name, location, leading, trailing)
		{
			Name = name;
		}


		public override SymbolToken WithTrivia(
			List<TriviaToken> leading, List<TriviaToken> trailing)
		{
			return new IdentifierToken(Name, Span, leading, trailing);
		}
	}
}
