using System.Collections.Generic;
using Mono.Cecil;
using ILPatcher.Model;

namespace ILPatcher.Assembly
{
	public class Root : IRoot
	{
		private readonly AssemblyDefinition _essence;
		private readonly Dictionary<TypePath, TypeHandle> _types = new Dictionary<TypePath, TypeHandle>();
		private Lookup<TypePath, TypeHandle> _readonlyTypes;

		public ILookup<TypePath, TypeHandle> Types =>
			_readonlyTypes ?? (_readonlyTypes = new Lookup<TypePath, TypeHandle>(_types));

		ILookup<TypePath, IType> IRoot.Types => Types;


		public Root(AssemblyDefinition backbone)
		{
			_essence = backbone;
			foreach (var module in backbone.Modules)
			{
				foreach (var type in module.Types)
				{
					var @namespace = GetNamespace(type.Namespace);
					var typeHandle = TypeHandle.Create(type, @namespace);
					_types[typeHandle.FullName] = typeHandle;
				}
			}
		}

		public TypePath GetNamespace(string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;
			string[] subSpaces = name.Split('.');
			var @namespace = new TypePath(subSpaces[0]);
			for (int i = 1; i < subSpaces.Length; i++)
			{
				@namespace = new TypePath(subSpaces[i], @namespace);
			}
			return @namespace;
		}
	}
}
