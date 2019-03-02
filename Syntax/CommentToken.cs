using System.Text;

namespace ILPatcher.Syntax
{
	public class CommentToken : TriviaToken
	{
		public readonly string Opener;
		public readonly string Comment;

		public override string Identifier => Opener + Comment;

		public CommentToken(string opener, string comment, Span location)
			: base(TokenType.Comment, location)
		{
			Opener = opener;
			Comment = comment;
		}
	}
}
