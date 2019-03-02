using System.Text;

namespace ILPatcher.Syntax
{
	public readonly struct Position
	{
		public readonly uint Column;
		public readonly uint Row;

		public Position(uint row, uint column)
			=> (Row, Column) = (row, column);


		public override string ToString()
		{
			var text = new StringBuilder();
			WriteTo(text);
			return text.ToString();
		}

		public StringBuilder WriteTo(StringBuilder text)
		{
			text.Append('(');
			text.Append(Row);
			text.Append(", ");
			text.Append(Column);
			return text.Append(')');
		}


		public static bool operator <(in Position left, in Position right)
		{
			return left.Row < right.Row ||
				(left.Row == right.Row && left.Column < right.Column);
		}

		public static bool operator >(in Position left, in Position right)
		{
			return left.Row > right.Row ||
				(left.Row == right.Row && left.Column > right.Column);
		}

		public static bool operator ==(in Position left, in Position right)
		{
			return left.Row == right.Row && left.Column == right.Column;
		}

		public static bool operator !=(in Position left, in Position right)
		{
			return left.Row != right.Row || left.Column != right.Column;
		}
	}
}
