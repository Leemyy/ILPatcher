using System;
using System.Collections.Generic;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class NamespaceHandle : INamespace
	{
		private string _trueName;
		private readonly Dictionary<string, NamespaceHandle> _subSpaces;
		private readonly List<TypeHandle> _members;

		public string Name { get; private set; }
		public string Identifier => Name;
		public bool IsUnspeakable { get; }
		public NamespaceHandle Parent { get; }
		public IReadOnlyCollection<NamespaceHandle> SubSpaces => _subSpaces.Values;
		public IReadOnlyList<TypeHandle> Types => _members.AsReadOnly();

		INamespace INamespace.Parent => Parent;
		IEnumerable<INamespace> INamespace.SubSpaces => SubSpaces;
		IEnumerable<IType> INamespace.Types => Types;


		public NamespaceHandle(string name, NamespaceHandle parent)
		{
			_trueName = name;
			Name = name;
			var index = Name.IndexOf('<');
			IsUnspeakable = index >= 0 && Name.IndexOf('>') > index;
			Parent = parent;
			_subSpaces = new Dictionary<string, NamespaceHandle>();
			_members = new List<TypeHandle>();
		}


		public NamespaceHandle SubSpace(string childName)
		{
			if (_subSpaces.TryGetValue(childName, out var child))
				return child;
			child = new NamespaceHandle(childName, this);
			_subSpaces[childName] = child;
			return child;
		}

		public void AddMember(TypeHandle type)
		{
			//TODO: why a dictionary?
			_members.Add(type);
		}

		public void Rename(string name)
		{
			_trueName = name;
			string fullNamespace = "";
			var enclosing = Parent;
			while (enclosing != null)
			{
				fullNamespace = enclosing._trueName + "." + fullNamespace;
				enclosing = enclosing.Parent;
			}
			RenameChildren(fullNamespace);
		}

		private void RenameChildren(string fullNamespace)
		{
			fullNamespace += "." + _trueName;
			for (int i = 0; i < _members.Count; i++)
			{
				_members[i].MoveTo(fullNamespace);
			}
			foreach (var subSpace in _subSpaces.Values)
			{
				subSpace.RenameChildren(fullNamespace);
			}
		}
	}
}
