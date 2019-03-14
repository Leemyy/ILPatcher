using System.Collections.Generic;

namespace ILPatcher.Model
{
	public interface ILookup<TKey, out TValue>
	{
		TValue this[TKey key] { get; }

		IEnumerable<TKey> Keys { get; }
		IEnumerable<TValue> Values { get; }

		bool ContainsKey(TKey key);
		TValue GetValue(TKey key, out bool success);
	}

	public class Lookup<TKey, TValue> : ILookup<TKey, TValue>
	{
		private Dictionary<TKey, TValue> _core;

		public Lookup(Dictionary<TKey, TValue> dict)
		{
			_core = dict;
		}

		public TValue this[TKey key] => _core[key];

		public IEnumerable<TKey> Keys => _core.Keys;

		public IEnumerable<TValue> Values => _core.Values;

		public bool ContainsKey(TKey key)
		{
			return _core.ContainsKey(key);
		}

		public TValue GetValue(TKey key, out bool success)
		{
			success = _core.TryGetValue(key, out var value);
			return value;
		}
	}
}
