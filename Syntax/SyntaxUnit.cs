using System.Text;

namespace ILPatcher.Syntax
{
	public class SyntaxUnit
	{
		public readonly Span Span;

		internal SyntaxUnit(Span location)
			=> Span = location;
	}
}
