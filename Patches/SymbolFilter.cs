using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public delegate bool SymbolFilter(ISymbol symbol);

	public static class FilterHelpers
	{
		public static void FilterWith<T>(this List<T> list, SymbolFilter remove) where T : SymbolPatch
		{
			for (int i = 0; i < list.Count; i++)
			{
				var type = list[i];
				if (remove(type))
				{
					//Replace current element with last element.
					list[i] = list[list.Count - 1];
					list.RemoveAt(list.Count - 1);
					//The current element got removed,
					// so the next one will be at the same index.
					i--;
					continue;
				}
				//else
				//{
				//	type.Filter(remove);
				//}
			}
		}
	}
}
