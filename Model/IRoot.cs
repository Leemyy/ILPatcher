using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Model
{
	public interface IRoot
	{
		IEnumerable<INamespace> Namespaces { get; }
	}
}
