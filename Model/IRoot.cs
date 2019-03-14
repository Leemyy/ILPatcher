using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Model
{
	public interface IRoot
	{
		ILookup<TypePath, IType> Types { get; }
	}
}
