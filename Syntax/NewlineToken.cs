using System.Text;

namespace ILPatcher.Syntax
{
	public class NewlineToken : TriviaToken
	{
		public readonly string Stopper;

		public override string Identifier { get; }

		public NewlineToken(int codepoint, int codepointDouble, Span location)
			: base(TokenType.Newline, location)
		{
			if (codepoint == 0x000A)
			{
				if (codepointDouble == 0x000D)
				{
					Identifier = "\\n\\r";
					Stopper = "\n\r";
				}
				else if (codepointDouble == -1)
				{
					Identifier = "\\n";
					Stopper = "\n";
				}
			}
			else if (codepoint == 0x000D)
			{
				if (codepointDouble == 0x000A)
				{
					Identifier = "\\r\\n";
					Stopper = "\r\n";
				}
				else if (codepointDouble == -1)
				{
					Identifier = "\\r";
					Stopper = "\r";
				}
			}
			if (Identifier is null)
			{
				Identifier = IdentifierFor(codepoint) + IdentifierFor(codepointDouble);
				Stopper = char.ConvertFromUtf32(codepoint);
				if (codepointDouble != -1)
					Stopper += char.ConvertFromUtf32(codepointDouble);
			}
		}


		private static string IdentifierFor(int codepoint)
		{
			if (codepoint == -1)
				return string.Empty;
			else if (codepoint == 0x000A)
				return "\\n";
			else if (codepoint == 0x000D)
				return "\\r";
			else
				return $"\\u{codepoint:X4}";
		}
	}
}
