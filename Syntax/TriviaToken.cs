using System.Text;

namespace ILPatcher.Syntax
{
	public abstract class TriviaToken : Token
	{
		internal TriviaToken(TokenType type, Span location)
			: base(type, location)
		{
		}
	}
}
