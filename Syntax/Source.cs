using System;
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
		private SymbolToken? _unexpected;
		private HashSet<string> _expected = new HashSet<string>();
		private int _lastSkip = -1;

		public int Length => _symbols.Length;
		public int Position => _position;
		public bool HasErrors => _errors.Count > 0;


		public Source(System.IO.FileInfo file, IList<SymbolToken> symbols)
		{
			File = file;
			_symbols = new SymbolToken[symbols.Count];
			symbols.CopyTo(_symbols, 0);
		}

		private Source(System.IO.FileInfo file, List<SymbolToken> symbols)
			=> (File, _symbols) = (file, symbols.ToArray());


		public IdentifierToken? ExpectIdentifier()
		{
			if (_symbols[_position] is IdentifierToken token)
			{
				_position++;
				return token;
			}
			Mismatch(_symbols[_position], "an identifier");
			return null;
		}

		public IdentifierToken? ExpectIdentifier(string identifier)
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

		public ControlToken? ExpectControl(char control)
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

		public EndOfFileToken? ExpectEnd()
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

		public void PrintErrors(System.IO.TextWriter writer)
		{
			if (HasErrors)
				writer.WriteLine($"Errors in file \"{File.FullName}\":");
			foreach (var error in _errors)
			{
				writer.Write("\t");
				writer.WriteLine(error);
			}
		}


		public static SyntaxTree? Parse(System.IO.FileInfo file)
		{
			var tokens = Lexer.Tokenize(file);
			var source = new Source(file, Lexer.BindTrivia(tokens));
			tokens = null!;
			var tree = SyntaxTree.Parse(source);
			source.PrintErrors(Console.Out);
			return tree;
		}
	}

	public class ParseError
	{
		public readonly SymbolToken Token;
		public readonly string Message;

		public ParseError(SymbolToken token, HashSet<string> expected)
		{
			Token = token;
			if (expected.Count == 1)
				Message = expected.First();
			else
			{
				var text = new StringBuilder();
				int remaining = expected.Count - 1;
				bool consecutive = false;
				foreach (var expect in expected)
				{
					if (consecutive)
						text.Append(", ");
					else
						consecutive = true;
					if (remaining-- == 0)
						text.Append("or ");
					text.Append('"');
					text.Append(expect);
					text.Append('"');
				}
				Message = text.ToString();
			}
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			text.Append("Invalid token \"");
			text.Append(Token.Identifier);
			text.Append("\" @ ");
			Token.Span.Start.WriteTo(text);
			text.Append(": expected ");
			text.Append(Message);
			text.Append('.');
			return text.ToString();
		}
	}
}
