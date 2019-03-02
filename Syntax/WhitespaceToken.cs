using System.Text;

namespace ILPatcher.Syntax
{
	public class WhitespaceToken : TriviaToken
	{
		public readonly string Spacer;
		public readonly uint Repeat;

		public override string Identifier { get; }

		public WhitespaceToken(int codepoint, uint repeat, Span location)
			: base(TokenType.Whitespace, location)
		{
			Spacer = char.ConvertFromUtf32(codepoint);
			Repeat = repeat;
			if (codepoint == 0x0009)
				Identifier = MakeTabIdentifier(repeat);
			else if (codepoint == 0x0020)
				Identifier = MakeSpaceIdentifier(repeat);
			else
				Identifier = MakeIdentifier($"\\u{codepoint:X4}", repeat);
		}


		private static string MakeTabIdentifier(uint repeat)
		{
			if (repeat <= 1)
				return "tab";
			switch (repeat)
			{
			case 2:
				return "tab×2";
			case 3:
				return "tab×3";
			case 4:
				return "tab×4";
			case 5:
				return "tab×5";
			case 6:
				return "tab×6";
			case 7:
				return "tab×7";
			case 8:
				return "tab×8";
			default:
				return MakeIdentifier("tab", repeat);
			}
		}

		private static string MakeSpaceIdentifier(uint repeat)
		{
			if (repeat <= 1)
				return "space";
			switch (repeat)
			{
			case 2:
				return "space×2";
			case 3:
				return "space×3";
			case 4:
				return "space×4";
			case 5:
				return "space×5";
			case 6:
				return "space×6";
			case 7:
				return "space×7";
			case 8:
				return "space×8";
			case 12:
				return "space×12";
			case 16:
				return "space×16";
			case 20:
				return "space×20";
			case 24:
				return "space×24";
			default:
				return MakeIdentifier("space", repeat);
			}
		}

		private static string MakeIdentifier(string prefix, uint repeat)
		{
			if (repeat <= 1)
				return prefix;
			return string.Concat(prefix, "×", repeat.ToString());	
		}
	}
}
