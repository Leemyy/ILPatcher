using System.IO;
using System.Text;

namespace ILPatcher.Syntax
{
	public readonly struct Span
	{
		public readonly Position Start;
		public readonly Position End;
		//public readonly FileInfo File;

		public Span(Position start, Position end)
			=> (Start, End) = (start, end);

		public Span(Position position)
			=> (Start, End) = (position, position);

		public Span(uint row, uint column)
			=> Start = End = new Position(row, column);


		public override string ToString()
		{
			var text = new StringBuilder();
			WriteTo(text);
			return text.ToString();
		}

		public StringBuilder WriteTo(StringBuilder text)
		{
			text.Append('｢');
			if (Start.Row == End.Row)
			{
				text.Append(Start.Row);
				text.Append(", ");
				text.Append(Start.Column);
				if (Start.Column != End.Column)
				{
					text.Append('-');
					text.Append(End.Column);
				}
			}
			else
			{
				text.Append('(');
				text.Append(Start.Row);
				text.Append(", ");
				text.Append(Start.Column);
				text.Append(")-(");
				text.Append(End.Row);
				text.Append(", ");
				text.Append(End.Column);
				text.Append(')');
			}
			return text.Append('｣');
		}
	}
}
