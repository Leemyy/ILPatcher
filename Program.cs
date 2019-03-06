using System;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using ILPatcher.Assembly;
using ILPatcher.Patches;
using ILPatcher.Model;
using ILPatcher.Emit;
using ILPatcher.Syntax;

namespace ILPatcher
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			string readPath = @"..\..\";//@"..\Dump\ILPatcher\TypeLiteral.nsp";
			
			var all = ParseAll(new DirectoryInfo(readPath));

			string filePath = @"..\Release\ILPatcher.exe";
			if (args?.Length > 0)
				filePath = args[0];
			var fileInfo = new FileInfo(filePath);
			//if (!fileInfo.Exists)
			//	Environment.Exit(1);

			int trim = fileInfo.Extension.Length;
			if (trim > 0)
				trim += 1;
			string fileName = fileInfo.Name.Remove(fileInfo.Name.Length - trim);
			string outputPath = Path.Combine(fileInfo.DirectoryName, fileInfo.Name) + ".renamed" + (trim > 0 ? "." + fileInfo.Extension : "");
			var outputInfo = new FileInfo(outputPath);
			//if (outputInfo.Exists)
			//	Environment.Exit(2);

			AssemblyDefinition asm;
			using (var read = fileInfo.Open(FileMode.Open))
			{
				asm = AssemblyDefinition.ReadAssembly(read);
			}

			var patch = new Patchwork(new Root(asm));
			patch.Filter(s => s.IsUnspeakable);
			patch.Filter(s =>
			{
				var method = (s as IMethod);
				if (method is null)
					return false;
				if (method.IsConstructor || method.IsGetter || method.IsSetter)
					return true;
				return method.IsOverride;
			});
			PatchWriter.Create(patch, Directory.CreateDirectory(@"..\Dump"));

			//using (var write = outputInfo.Open(FileMode.Create))
			//{
			//	asm.Write(write/*, new WriterParameters { SymbolWriterProvider = AssemblyManager.SymbolWriterProvider.instance }*/);
			//}
		}

		public static SyntaxTree[] ParseAll(DirectoryInfo dir)
		{
			var files = dir.GetFiles("*.nsp", SearchOption.AllDirectories);
			var trees = new SyntaxTree[files.Length];
			for (int i = 0; i < files.Length; i++)
			{
				trees[i] = Source.Parse(files[i]);
			}
			return trees;
		}
	}
}
