using System.Collections.Generic;
using System.IO;
using System.Text;
using ILPatcher.Syntax;
using static ILPatcher.Syntax.TokenType;

namespace ILPatcher.Emit
{
	public static class Lexer
	{
		public static List<Token> Tokenize(FileInfo file)
		{
			using (var reader = file.OpenText())
				return Tokenize(reader);
		}

		private static List<Token> Tokenize(StreamReader reader)
		{
			var tokens = new List<Token>();
			var buffer = new StringBuilder();
			string commentTag = "#";
			var start = new Position(1, 1);
			uint col = 1;
			uint row = 1;
			var state = Control;
			for (int code = reader.Read(); code != -1; col++, code = reader.Read())
			{
				var targetMode = GetTokenType(code);
				//Handle escaped and commented out characters
				if (targetMode != Newline)
				{
					if (state == Escape)
					{
						targetMode = Name;
						state = Name;
					}
					else if (state == Comment)
					{
						buffer.Append(char.ConvertFromUtf32(code));
						continue;
					}
					else if (targetMode == Escape)
					{
						state = Escape;
						continue;
					}
				}
				else if (state == Escape)
				{
					//TODO: Warn about inescapable line breaks
					if (buffer.Length > 0)
						state = Name;
					else
						state = Control;
				}
				//Close multi-character Tokens
				if (state == Name && targetMode != Name)
				{
					tokens.Add(new IdentifierToken(buffer.ToString(),
						new Span(start, new Position(row, col - 1))));
					buffer.Clear();
				}
				else if (state == Comment && targetMode != Comment)
				{
					tokens.Add(new CommentToken(commentTag, buffer.ToString(),
						new Span(start, new Position(row, col - 1))));
					buffer.Clear();
				}
				//Append character to new token
				if (state != targetMode)
				{
					start = new Position(row, col);
					state = targetMode;
				}
				string character = char.ConvertFromUtf32(code);
				switch (targetMode)
				{
				case Control:
					tokens.Add(new ControlToken(character[0], new Span(row, col)));
					break;
				case Newline:
					tokens.Add(LineBreakToken(code, reader, new Position(row, col)));
					row++;
					col = 0;
					break;
				case Whitespace:
					uint count = 1;
					while (reader.Peek() == code)
					{
						count++;
						reader.Read();
					}
					col += count - 1;
					tokens.Add(new WhitespaceToken(code, count,
						new Span(start, new Position(row, col))));
					buffer.Clear();
					break;
				case Comment:
					commentTag = character;
					break;
				case Name:
					buffer.Append(character);
					break;
				}
			}
			if (state == Name)
				tokens.Add(new IdentifierToken(buffer.ToString(),
					new Span(start, new Position(row, col - 1))));
			else if (state == Comment)
				tokens.Add(new CommentToken(commentTag, buffer.ToString(),
					new Span(start, new Position(row, col - 1))));
			return tokens;
		}

		public static TokenType GetTokenType(int code)
		{
			switch (code)
			{
			case 0x000A: // Line Feed
			case 0x000B: // Vertical Tab
			case 0x000C: // Form Feed
			case 0x000D: // Carriage Return
			case 0x0085: // Next Line
			case 0x2028: // Line Separator
			case 0x2029: // Paragraph Separator
				return Newline;
			case 0x0009: // Horizontal Tab
			case 0x0020: // Space
			case 0x00A0: // NO-BREAK SPACE
			case 0x1680: // OGHAM SPACE MARK
			case 0x2000: // EN QUAD
			case 0x2001: // EM QUAD
			case 0x2002: // EN SPACE
			case 0x2003: // EM SPACE
			case 0x2004: // THREE-PER-EM SPACE
			case 0x2005: // FOUR-PER-EM SPACE
			case 0x2006: // SIX-PER-EM SPACE
			case 0x2007: // FIGURE SPACE
			case 0x2008: // PUNCTUATION SPACE
			case 0x2009: // THIN SPACE
			case 0x200A: // HAIR SPACE
			case 0x202F: // NARROW NO-BREAK SPACE
			case 0x205F: // MEDIUM MATHEMATICAL SPACE
			case 0x3000: // IDEOGRAPHIC SPACE
				return Whitespace;
			case '#':
				return Comment;
			case '*':
				return Escape;
			case '=':
			case '<':
			case '>':
			case '(':
			case ')':
			case '{':
			case '}':
			case '[':
			case ']':
			case '.':
			case ':':
			case ',':
			case ';':
			case '~':
			case '?':
				return Control;
			default:
				return Name;
			}
		}

		private static NewlineToken LineBreakToken(int code, StreamReader reader, Position start)
		{
			int next = reader.Peek();
			//Detect multi-character newlines
			if (code == 0x000A && next == 0x000D)
			{
				//Consume the second character
				return new NewlineToken(code, reader.Read(),
					new Span(start, new Position(start.Row, start.Column+1)));
			}
			else if (code == 0x000D && next == 0x000A)
			{
				//Consume the second character
				return new NewlineToken(code, reader.Read(),
					new Span(start, new Position(start.Row, start.Column+1)));
			}
			return new NewlineToken(code, -1, new Span(start));
		}

		public static List<SymbolToken> BindTrivia(List<Token> tokens)
		{
			var bound = new List<SymbolToken>();
			var head = new List<TriviaToken>();
			var tail = new List<TriviaToken>();
			SymbolToken symbol = null;
			int count = tokens.Count;
			int i = 0;
			//Find first symbol
			for (; i < count; i++)
			{
				var token = tokens[i];
				if (token is TriviaToken t)
					head.Add(t);
				else if (token is SymbolToken s)
				{
					symbol = s;
					break;
				}
			}
			//Return empty list if there are no symbols
			if (symbol is null)
			{
				bound.Add(new EndOfFileToken(new Span(1, 1)));
				return bound;
			}
			i++;
			bool inTail = true;
			for (; i < count; i++)
			{
				var token = tokens[i];
				if (token is TriviaToken t)
				{
					if (inTail)
					{
						tail.Add(t);
						if (t is NewlineToken)
						{
							bound.Add(symbol.WithTrivia(head, tail));
							head.Clear();
							tail.Clear();
							inTail = false;
						}
					}
					else
						head.Add(t);
					continue;
				}
				var nextSymbol = token as SymbolToken;
				if (nextSymbol is null)
					continue;
				if (inTail)
				{
					bound.Add(symbol.WithTrivia(head, tail));
					head.Clear();
					tail.Clear();
				}
				symbol = nextSymbol;
				inTail = true;
			}
			//Add the last symbol
			if (inTail)
			{
				bound.Add(symbol.WithTrivia(head, tail));
			}
			else if (head.Count > 0)
			{
				//The last symbol was already added,
				// but there is more trivia that needs to be appended to it.
				var last = bound.Count-1;
				symbol = bound[last];
				tail.AddRange(symbol.TrailingTrivia);
				tail.AddRange(head);
				head.Clear();
				head.AddRange(symbol.LeadingTrivia);
				bound[last] = symbol.WithTrivia(head, tail);
			}
			var end = symbol.Span.End;
			bound.Add(new EndOfFileToken(new Span(end.Row, end.Column + 1)));
			return bound;
		}
	}
}
