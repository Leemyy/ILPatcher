using ILPatcher.Model;

namespace ILPatcher.Emit
{
	public readonly struct Patch
	{
		public readonly ISymbol Symbol;
		public readonly string Namechange;

		public Patch(ISymbol symbol, string namechange)
		{
			Symbol = symbol;
			Namechange = namechange;
		}

		public void Apply()
		{
			Symbol.Rename(Namechange);
		}
	}
}
