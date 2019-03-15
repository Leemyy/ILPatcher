using System;
using System.Collections.Generic;
using System.Text;
using ILPatcher.Model;

namespace ILPatcher
{
	public class MethodGroup<T> where T : IMethod
	{
		private static readonly int[] NoHashes = new int[0];
		private static readonly T[] NoOverloads = new T[0];

		private int _count;
		private int[] _hashes;
		private T[] _overloads;

		public string Name { get; }
		public int Count => _count;

		public MethodGroup()
		{
			_hashes = NoHashes;
			_overloads = NoOverloads;
		}


		public void AddOverload(T method)
		{
			int slots = method.OptionalParameterCount + 1;
			if (_count == _overloads.Length)
				GrowBackingArrays(_count + slots);

			var parameters = method.Parameters;
			int hash = method.GenericParameterCount;
			var nonOptional = parameters.Count - method.OptionalParameterCount;
			for (int i = 0; i < nonOptional; i++)
			{
				hash ^= parameters[i].Type.GetHashCode();
			}

			for (int i = 0; i < slots; i++)
			{
				if (i > 0)
					hash ^= parameters[nonOptional + i - 1].Type.GetHashCode();

				_overloads[_count + i] = method;
				_hashes[_count + i] = hash;
			}
			_count += slots;
		}

		private void GrowBackingArrays(int min)
		{
			var newSize = _count * 2;
			if (newSize < min)
				newSize = min;
			var growHashes = new int[newSize];
			var growOverloads = new T[newSize];
			Array.Copy(_hashes, growHashes, _count);
			Array.Copy(_overloads, growOverloads, _count);
			_hashes = growHashes;
			_overloads = growOverloads;
		}
	}
}
