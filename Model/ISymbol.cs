namespace ILPatcher.Model
{
	public interface ISymbol
	{
		string Name { get; }
		string Identifier { get; }

		bool IsUnspeakable { get; }
		//bool IsCompilerGenerated { get; }
		//bool IsSpecialName { get; }
		//bool IsRuntimeName { get; }

		void Rename(string name);
	}
}
