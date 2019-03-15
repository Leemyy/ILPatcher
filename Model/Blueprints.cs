using System.Collections.Generic;

namespace ILPatcher.Model
{
	public interface IType : ISymbol
	{
		TypePath FullName { get; }
	}

	public interface IEnum : IType
	{
		TypeLiteral BaseType { get; }
		ILookup<string, IEnumConstant> Constants { get; }
	}

	public interface IEnumConstant : ISymbol
	{
		object Value { get; }
	}

	public interface IParametrizedType : IType
	{
		IReadOnlyList<IGenericParameter> GenericParameters { get; }
	}

	public interface IDelegate : IParametrizedType
	{
		IReadOnlyList<IParameter> Parameters { get; }
		TypeLiteral ReturnType { get; }
	}

	public interface IMemberType : IParametrizedType
	{
		//IEnumerable<string> Interfaces { get; }
		ILookup<string, IProperty> Properties { get; }
		ILookup<string, IEvent> Events { get; }
		ILookup<string, IMethod> Methods { get; }
	}

	public interface IInterface : IMemberType
	{
	}

	public interface IDataType : IMemberType
	{
		ILookup<string, IType> Nested { get; }
		ILookup<string, IField> Fields { get; }
	}

	public interface IStruct : IDataType
	{
	}

	public interface IClass : IDataType
	{
		TypeLiteral BaseClass { get; }
	}

	public interface IGenericParameter : ISymbol
	{
		GenericVariance Variance { get; }
	}

	public interface IField : ISymbol
	{
		TypeLiteral Type { get; }
		bool IsConstant { get; }
	}

	public interface IEvent : ISymbol
	{
		TypeLiteral Type { get; }
		bool IsOverride { get; }
	}

	public interface IProperty : ISymbol
	{
		//TODO: indexers
		TypeLiteral Type { get; }
		bool IsOverride { get; }
		bool HasGet { get; }
		bool HasSet { get; }
	}

	public interface IMethod : ISymbol
	{
		IReadOnlyList<IGenericParameter> GenericParameters { get; }
		int GenericParameterCount { get; }
		IReadOnlyList<IParameter> Parameters { get; }
		int ParameterCount { get; }
		TypeLiteral ReturnType { get; }
		int OptionalParameterCount { get; }
		bool IsConstructor { get; }
		bool IsOverride { get; }
		bool IsGetter { get; }
		bool IsSetter { get; }
		bool IsExtension { get; }
	}

	public interface IParameter : ISymbol
	{
		TypeLiteral Type { get; }
		ParameterPolicy Policy { get; }
		bool IsOptional { get; }
	}
}
