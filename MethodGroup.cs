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
		private int _optionalCount;
		private int[] _optionalHashes;
		private T[] _optionalOverloads;

		public string Name { get; }
		public int Count => _count;

		public MethodGroup()
		{
			_hashes = NoHashes;
			_overloads = NoOverloads;
			_optionalHashes = NoHashes;
			_optionalOverloads = NoOverloads;
		}


		public void AddOverload(T method)
		{
			if (_count == _overloads.Length)
				GrowBackingArrays(_count + 1);

			_overloads[_count] = method;
			_hashes[_count] = HashOf(method, method.ParameterCount);

			_count++;

			AddOptionalOverloads(method);
		}

		private void AddOptionalOverloads(T method)
		{
			var count = method.OptionalParameterCount;
			if (count == 0)
				return;
			if (_optionalCount + count > _overloads.Length)
				GrowOptionalArrays(_optionalCount + count);

			var nonOptional = method.ParameterCount - count;
			for (int i = 0; i < count; i++)
			{
				_optionalHashes[_optionalCount + i] = HashOf(method, nonOptional + i);
				_optionalOverloads[_optionalCount + i] = method;
			}
			_optionalCount += count;
		}

		public bool ConflictsWith(IMethod method)
		{
			var count = method.ParameterCount;
			var nonOptional = count - method.OptionalParameterCount;
			for (int i = nonOptional; i <= count; i++)
			{
				if (ConflictsWith(method, HashOf(method, i), i))
					return true;
			}
			return false;
		}

		private bool ConflictsWith(IMethod method, int hash, int parameterCount)
		{
			for (int i = 0; i < _optionalHashes.Length; i++)
			{
				if (_optionalHashes[i] != hash)
					continue;
				if (!SignaturesMatch(_optionalOverloads[i], method))
					continue;
				return true;
			}
			for (int i = 0; i < _hashes.Length; i++)
			{
				if (_hashes[i] != hash)
					continue;
				if (!SignaturesMatch(_overloads[i], method))
					continue;
				return true;
			}
			return false;
		}

		public T FindMatch(IMethod method)
		{
			var hash = HashOf(method, method.ParameterCount);
			T match = default;
			bool exact = true;
			for (int i = 0; i < _hashes.Length; i++)
			{
				if (_hashes[i] != hash)
					continue;
				if (!SignaturesMatch(_overloads[i], method))
					continue;

				if (match == default)
					match = _overloads[i];
				else
				{
					if (exact)
					{
						exact = false;
						Console.WriteLine($"Ambiguous match, {method} matches more than one method:");
						Console.Write("\t- ");
						Console.WriteLine(match.ToString());
					}
					Console.Write("\t- ");
					Console.WriteLine(_overloads[i].ToString());
				}
			}

			if (exact)
				return match;
			return default;
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

		private void GrowOptionalArrays(int min)
		{
			var newSize = _optionalCount * 2;
			if (newSize < min)
				newSize = min;
			var growHashes = new int[newSize];
			var growOverloads = new T[newSize];
			Array.Copy(_optionalHashes, growHashes, _optionalCount);
			Array.Copy(_optionalOverloads, growOverloads, _optionalCount);
			_optionalHashes = growHashes;
			_optionalOverloads = growOverloads;
		}


		private static int HashOf(IMethod method, int parameterCount)
		{
			int hash = method.GenericParameterCount;
			if (parameterCount == 0)
				return hash;
			var parameters = method.Parameters;
			for (int i = 0; i < parameterCount; i++)
			{
				hash ^= parameters[i].Type.GetHashCode();
			}
			return hash;
		}

		private static bool SignaturesMatch(T left, IMethod right)
		{
			if (left.GenericParameterCount != right.GenericParameterCount)
				return false;
			if (left.ParameterCount != right.ParameterCount)
				return false;

			var leftParams = left.Parameters;
			var rightParams = right.Parameters;
			var count = leftParams.Count;
			for (int i = 0; i < count; i++)
			{
				if (leftParams[i].Policy.IsByRef() != rightParams[i].Policy.IsByRef() ||
					leftParams[i].Type != rightParams[i].Type)
					return false;
			}

			return true;
		}
	}
}
