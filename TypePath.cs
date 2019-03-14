using System;
using System.Collections.Generic;
using System.Text;

namespace ILPatcher
{
	public class TypePath : IEquatable<TypePath>
	{
		private static readonly string[] NoParams = new string[0];
		private static readonly IReadOnlyList<string> NoParamsReadonly = Array.AsReadOnly(NoParams);

		public readonly string Name;
		public readonly TypePath Parent;
		private readonly string[] _parameters;

		public bool HasParent => !(Parent is null);
		public int Count => _parameters.Length;
		public string this[int index] => _parameters[index];
		public IReadOnlyList<string> Parameters =>
			_parameters == NoParams ? NoParamsReadonly : Array.AsReadOnly(_parameters);

		public TypePath(string name, TypePath parent = null, string[] parameters = null)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			if (name.Length == 0)
				throw new ArgumentException("Name must not be empty", nameof(name));
			Parent = parent;
			if (parameters is null || parameters.Length == 0)
			{
				_parameters = NoParams;
				return;
			}

			var copy = new string[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				var par = parameters[i];
				if (par is null)
					throw new ArgumentException("The parameters array must not contain null", nameof(parameters));
				copy[i] = par;
			}
			_parameters = copy;
		}


		public bool Equals(TypePath other)
		{
			if ((object)this == other)
				return true;
			if (other is null)
				return false;
			if (_parameters.Length != other._parameters.Length)
				return false;
			if (Name != other.Name)
				return false;
			if (Parent != other.Parent)
				return false;

			return true;
		}

		public static bool operator ==(TypePath left, TypePath right)
		{
			if (left is null)
				return right is null;
			return left.Equals(right);
		}

		public static bool operator !=(TypePath left, TypePath right)
		{
			if (left is null)
				return !(right is null);
			return !left.Equals(right);
		}

		public override bool Equals(object other)
		{
			return Equals(other as TypePath);
		}

		public override int GetHashCode()
		{
			int hash = Name.GetHashCode();
			if (HasParent)
				hash ^= Parent.GetHashCode();
			hash ^= _parameters.Length;
			return hash;
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			return WriteTo(text).ToString();
		}

		public virtual StringBuilder WriteTo(StringBuilder text)
		{
			if (HasParent)
			{
				Parent.WriteTo(text);
				text.Append('.');
			}
			text.Append(Name);
			if (_parameters.Length == 0)
				return text;
			text.Append('<');
			for (int i = 0; i < _parameters.Length; i++)
			{
				if (i > 0)
					text.Append(", ");
				text.Append(_parameters[i]);
			}
			text.Append('>');
			return text;
		}
	}
}
