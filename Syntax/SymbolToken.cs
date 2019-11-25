using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ILPatcher.Syntax
{
	public abstract class SymbolToken : Token
	{
		private static readonly ReadOnlyCollection<TriviaToken> None =
			new ReadOnlyCollection<TriviaToken>(new TriviaToken[0]);
		public readonly ReadOnlyCollection<TriviaToken> LeadingTrivia;
		public readonly ReadOnlyCollection<TriviaToken> TrailingTrivia;

		internal SymbolToken(TokenType type, Span location)
			: base(type, location)
		{
			LeadingTrivia = None;
			TrailingTrivia = None;
		}

		internal SymbolToken(TokenType type, Span location,
			List<TriviaToken> leading, List<TriviaToken> trailing)
			: base(type, location)
		{
			LeadingTrivia = leading is null || leading.Count == 0 ? None :
				new ReadOnlyCollection<TriviaToken>(leading.ToArray());
			TrailingTrivia = trailing is null || trailing.Count == 0 ? None :
				new ReadOnlyCollection<TriviaToken>(trailing.ToArray());
		}


		public abstract SymbolToken WithTrivia(
			List<TriviaToken> leading, List<TriviaToken> trailing);
	}
}
