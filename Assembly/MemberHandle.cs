using System;
using System.Collections.Generic;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public abstract class MemberHandle : ISymbol
	{
		private bool _unspeakable;
		private bool _unspeakableInitialized;
		private readonly MemberReference _essence;

		public virtual string Name { get; }
		public virtual string Identifier => Name;
		public bool IsUnspeakable
		{
			get
			{
				if (!_unspeakableInitialized)
				{
					_unspeakableInitialized = true;
					var index = Name.IndexOf('<');
					_unspeakable = index >= 0 && Name.IndexOf('>') > index;
				}
				return _unspeakable;
			}
		}


		public MemberHandle(MemberReference source)
		{
			_essence = source;
			Name = source.Name;
		}


		public void Rename(string name)
		{
			_essence.Name = name;
		}
	}
}
