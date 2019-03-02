using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILPatcher.Emit
{
	public sealed class Cursor : StreamWriter
	{
		private static readonly char[] DefaultIndentStyle = new char[] { '\t' };
		private static readonly char[] DefaultIndentBuffer = new char[] { '\t', '\t', '\t', '\t' };

		private int _indent;
		private char[] _indentStyle;
		private char[] _indentBuffer;
		private bool _lineStart;
		private int _spacing;
		private bool _blockStart;


		public Cursor(Stream stream) : base(stream, Encoding.UTF8)
		{
			_indent = 0;
			_indentStyle = DefaultIndentStyle;
			_indentBuffer = DefaultIndentBuffer;
		}

		public Cursor(string filePath) : base(filePath, false, Encoding.UTF8)
		{
			_indent = 0;
			_indentStyle = DefaultIndentStyle;
			_indentBuffer = DefaultIndentBuffer;
		}


		public void Indent()
		{
			_indent++;
			_blockStart = true;
			if (_indentBuffer.Length < _indent * _indentStyle.Length)
			{
				var resized = new char[_indentBuffer.Length * 2];
				Array.Copy(_indentBuffer, 0, resized, 0, _indentBuffer.Length);
				Array.Copy(_indentBuffer, 0, resized, _indentBuffer.Length, _indentBuffer.Length);
				_indentBuffer = resized;
			}
		}

		public void Unindent()
		{
			_spacing = 0;
			_blockStart = false;
			if (_indent > 0)
				_indent--;
		}

		public void Spacer(int lines)
		{
			if (!_blockStart && lines > _spacing)
				_spacing = lines;
		}

		public void IndentStyle(string style)
		{
			var newStyle = style.ToCharArray();
			if (newStyle.Length == _indentStyle.Length)
			{
				bool identical = true;
				for (int i = 0; i < newStyle.Length; i++)
				{
					if (newStyle[i] == _indentStyle[i])
						continue;
					identical = false;
					break;
				}
				if (identical)
					return;
				_indentStyle = newStyle;
				if (_indentBuffer == DefaultIndentBuffer)
					_indentBuffer = new char[_indentBuffer.Length];
			}
			else
			{
				int layers = _indentBuffer.Length / _indentStyle.Length;
				_indentStyle = newStyle;
				_indentBuffer = new char[layers * newStyle.Length];
			}
			for (int i = 0; i < _indentBuffer.Length; i+=_indentStyle.Length)
			{
				Array.Copy(_indentStyle, 0, _indentBuffer, i, _indentStyle.Length);
			}
		}

		public override void Write(char value)
		{
			if (_lineStart)
				WriteIndent();
			base.Write(value);
		}

		public override void Write(char[] buffer)
		{
			if (_lineStart)
				WriteIndent();
			base.Write(buffer);
		}

		public override void Write(char[] buffer, int index, int count)
		{
			if (_lineStart)
				WriteIndent();
			base.Write(buffer, index, count);
		}

		public override void Write(string value)
		{
			if (_lineStart)
				WriteIndent();
			base.Write(value);
		}

		private void WriteIndent()
		{
			//base.Write('「');
			_lineStart = false;
			for (int i = 0; i <= _spacing; i++)
			{
				if (i > 0)
					base.WriteLine();
				base.Write(_indentBuffer, 0, _indent * _indentStyle.Length);
			}
			_spacing = 0;
			//base.Write('」');
		}

		public override void WriteLine()
		{
			base.WriteLine();
			_blockStart = false;
			_lineStart = true;
		}

		public override void WriteLine(string value)
		{
			base.WriteLine(value);
			_blockStart = false;
			_lineStart = true;
		}
	}
}
