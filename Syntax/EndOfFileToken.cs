using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Syntax
{
	public class EndOfFileToken : SymbolToken
	{
		public override string Identifier => "End of File";

		public EndOfFileToken(Span location)
			: base(TokenType.Newline, location, null, null)
		{
		}

		private EndOfFileToken(Span location,
			List<TriviaToken> leading, List<TriviaToken> trailing)
			: base(TokenType.Newline, location, leading, trailing)
		{
		}


		public override SymbolToken WithTrivia(List<TriviaToken> leading, List<TriviaToken> trailing)
		{
			return new EndOfFileToken(Span, leading, trailing);
		}
	}
}
