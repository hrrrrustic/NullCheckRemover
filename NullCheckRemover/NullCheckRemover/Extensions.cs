using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover
{
    public static class Extensions
    {
        public static IEnumerable<IParameterSymbol> GetAvailableForAnalyzeParameters(this SimpleLambdaExpressionSyntax node, SemanticModel semantic) 
            => new List<ParameterSyntax>(1){node.Parameter}.WhereIsAvailableForAnalyze(semantic);

        public static IEnumerable<IParameterSymbol> GetAvailableForAnalyzeParameters(this BaseParameterListSyntax parameters, SemanticModel semantic)
            => parameters.Parameters.WhereIsAvailableForAnalyze(semantic);

        public static IEnumerable<IParameterSymbol> WhereIsAvailableForAnalyze(this IEnumerable<ParameterSyntax> parameters, SemanticModel semantic) 
            => parameters
                .Where(k => IsAvailableForAnalyze(k, semantic))
                .Select(k => semantic.GetDeclaredSymbol(k))
                .Where(k => k is not null)!;

        private static bool IsAvailableForAnalyze(ParameterSyntax? parameter, SemanticModel semantic)
        {
            var parameterType = parameter?.Type;
            if (parameterType is null)
                return false;

            var typeInfo = semantic.GetTypeInfo(parameterType);
            return IsReferenceType(typeInfo) || IsGenericWithoutTypeConstraint(typeInfo);
        }

        private static bool IsReferenceType(TypeInfo typeInfo) => typeInfo.Type is {IsReferenceType: true};

        private static bool IsGenericWithoutTypeConstraint(TypeInfo typeInfo) => typeInfo.Type is {IsReferenceType: false, IsValueType: false};
    }
}