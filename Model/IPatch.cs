using System;
using System.Collections.Generic;
using ILPatcher.Model;

namespace ILPatcher.Model
{
	public interface IPatch
	{
		string TargetName { get; }
	}
}
