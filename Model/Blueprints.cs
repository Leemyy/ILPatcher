using System.Collections.Generic;

namespace ILPatcher.Model
{
	public interface INamespace : ISymbol
	{
		INamespace Parent { get; }
		IEnumerable<INamespace> SubSpaces { get; }
		IEnumerable<IType> Types { get; }
	}

	public interface IType : ISymbol
	{
		INamespace Namespace { get; }
	}

	public interface IEnum : IType
	{
		TypeLiteral BaseType { get; }
		IEnumerable<IEnumConstant> Constants { get; }
	}

	public interface IEnumConstant : ISymbol
	{
		object Value { get; }
	}

	public interface IParametrizedType : IType
	{
		IEnumerable<IGenericParameter> GenericParameters { get; }
	}

	public interface IDelegate : IParametrizedType
	{
		IEnumerable<IParameter> Parameters { get; }
		TypeLiteral ReturnType { get; }
	}

	public interface IMemberType : IParametrizedType
	{
		IEnumerable<IProperty> Properties { get; }
		IEnumerable<IEvent> Events { get; }
		IEnumerable<IMethod> Methods { get; }
	}

	public interface IInterface : IMemberType
	{
		//IEnumerable<string> Interfaces { get; }
	}

	public interface IDataType : IMemberType
	{
		IEnumerable<IType> Nested { get; }
		IEnumerable<IField> Fields { get; }
	}

	public interface IStruct : IDataType
	{
		//IEnumerable<string> Interfaces { get; }
	}

	public interface IClass : IDataType
	{
		TypeLiteral BaseClass { get; }
		//IEnumerable<string> Interfaces { get; }
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
	}

	public interface IProperty : ISymbol
	{
		//TODO: indexers
		TypeLiteral Type { get; }
		bool HasGet { get; }
		bool HasSet { get; }
	}

	public interface IMethod : ISymbol
	{
		IEnumerable<IGenericParameter> GenericParameters { get; }
		IEnumerable<IParameter> Parameters { get; }
		TypeLiteral ReturnType { get; }
		int OptionalParameters { get; }
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
