using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Patches
{
	public class InterfacePatch : MemberTypePatch, IInterface
	{
		//public IEnumerable<string> Interfaces => throw new NotImplementedException();


		public InterfacePatch(IInterface source, NamespacePatch namespc)
			: base(source, namespc)
		{
		}
	}
}
