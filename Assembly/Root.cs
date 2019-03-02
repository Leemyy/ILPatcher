using System.Collections.Generic;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class Root : IRoot
	{
		private AssemblyDefinition _essence;
		private Dictionary<string, NamespaceHandle> _namespaces = new Dictionary<string, NamespaceHandle>();

		public IReadOnlyCollection<NamespaceHandle> Namespaces => _namespaces.Values;

		IEnumerable<INamespace> IRoot.Namespaces => _namespaces.Values;


		public Root(AssemblyDefinition backbone)
		{
			_essence = backbone;
			foreach (var module in backbone.Modules)
			{
				foreach (var type in module.Types)
				{
					var namespc = GetNamespace(type.Namespace);
					namespc.AddMember(TypeHandle.Create(type, namespc));
				}
			}
		}

		public NamespaceHandle GetNamespace(string name)
		{
			string[] subSpaces = name.Split('.');
			if (!_namespaces.TryGetValue(subSpaces[0], out var namespc))
			{
				namespc = new NamespaceHandle(subSpaces[0], null);
				_namespaces[subSpaces[0]] = namespc;
			}
			for (int i = 1; i < subSpaces.Length; i++)
			{
				namespc = namespc.SubSpace(subSpaces[i]);
			}
			return namespc;
		}
	}
}
