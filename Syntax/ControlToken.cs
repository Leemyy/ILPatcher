using System.Collections.Generic;
using System.Text;

namespace ILPatcher.Syntax
{
	public class ControlToken : SymbolToken
	{
		public readonly char Symbol;

		public override string Identifier { get; }

		public ControlToken(char symbol, Span location)
			: base(TokenType.Control, location)
		{
			Symbol = symbol;
			Identifier = symbol.ToString();
		}

		private ControlToken(char symbol, Span location,
			List<TriviaToken> leading, List<TriviaToken> trailing)
			: base(TokenType.Control, location, leading, trailing)
		{
			Symbol = symbol;
			Identifier = symbol.ToString();
		}


		public override SymbolToken WithTrivia(
			List<TriviaToken> leading, List<TriviaToken> trailing)
		{
			return new ControlToken(Symbol, Span, leading, trailing);
		}
	}
}
