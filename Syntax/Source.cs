using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILPatcher.Emit;

namespace ILPatcher.Syntax
{
	public class Source
	{
		private readonly System.IO.FileInfo File;
		private readonly List<ParseError> _errors = new List<ParseError>();
		private readonly SymbolToken[] _symbols;
		private int _position = 0;
		private SymbolToken _unexpected;
		private List<string> _expected = new List<string>();
		private int _lastSkip = -1;

		public int Length => _symbols.Length;
		public int Position => _position;


		private Source(System.IO.FileInfo file, List<SymbolToken> symbols)
			=> (File, _symbols) = (file, symbols.ToArray());


		public IdentifierToken ExpectIdentifier()
		{
			if (_symbols[_position] is IdentifierToken token)
			{
				_position++;
				return token;
			}
			Mismatch(_symbols[_position], "an identifier");
			return null;
		}

		public IdentifierToken ExpectIdentifier(string identifier)
		{
			if (_symbols[_position] is IdentifierToken token &&
				token.Name == identifier)
			{
				_position++;
				return token;
			}
			Mismatch(_symbols[_position], identifier);
			return null;
		}

		public ControlToken ExpectControl(char control)
		{
			if (_symbols[_position] is ControlToken token &&
				token.Symbol == control)
			{
				_position++;
				return token;
			}
			Mismatch(_symbols[_position], control.ToString());
			return null;
		}

		public EndOfFileToken ExpectEnd()
		{
			if (_symbols[_position] is EndOfFileToken token)
			{
				return token;
			}
			return null;
		}

		private void Mismatch(SymbolToken token, string expected)
		{
			if (_unexpected is null)
			{
				_unexpected = token;
				_expected.Add(expected);
				return;
			}
			if (token.Span.Start < _unexpected.Span.Start)
				return;
			if (token != _unexpected)
			{
				_unexpected = token;
				_expected.Clear();
			}
			_expected.Add(expected);
		}

		public bool SkipToken()
		{
			ParseError();
			_lastSkip = _position;

			if (_symbols[_position] is EndOfFileToken)
				return false;

			_position++;
			return true;
		}

		public void ParseError()
		{
			if (!(_unexpected is null))
			{
				if (_position > _lastSkip + 1)
					_errors.Add(new ParseError(_unexpected, _expected));
				_unexpected = null;
				_expected.Clear();
			}
		}

		public bool Reset(int position)
		{
			_position = position;
			return true;
		}


		public static SyntaxTree Parse(System.IO.FileInfo file)
		{
			var tokens = Lexer.Tokenize(file);
			var source = new Source(file, Lexer.BindTrivia(tokens));
			tokens = null;
			return SyntaxTree.Parse(source);
		}
	}

	public class ParseError
	{
		public readonly SymbolToken Token;
		public readonly string Message;

		public ParseError(SymbolToken token, List<string> expected)
			=> (Token, Message) = (token, string.Join(", ", expected.Distinct()));

		public override string ToString()
		{
			var text = new StringBuilder();
			text.Append(Message);
			text.Append(" @ ");
			Token.Span.Start.WriteTo(text);
			return text.ToString();
		}
	}
}
