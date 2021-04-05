using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover
{
    public static class Extensions
    {
        public static IEnumerable<IParameterSymbol> GetReferenceTypeParameters(this MethodDeclarationSyntax node, SemanticModel semantic) 
            => node.ParameterList.Parameters.WhereIsReferenceType(semantic);
        public static IEnumerable<IParameterSymbol> GetReferenceTypeParameters(this IndexerDeclarationSyntax node, SemanticModel semantic) 
            => node.ParameterList.Parameters.WhereIsReferenceType(semantic);

        public static IEnumerable<IParameterSymbol> WhereIsReferenceType(this IEnumerable<ParameterSyntax> parameters, SemanticModel semantic)
        {
            foreach (var parameter in parameters)
            {
                var parameterType = parameter.Type;
                if (parameterType is null)
                    continue;

                var typeInfo = semantic.GetTypeInfo(parameterType);
                if (typeInfo.Type is not {IsReferenceType:true})
                    continue;

                var symbol = semantic.GetDeclaredSymbol(parameter);
                if (symbol is not null)
                    yield return symbol;
            }
        }

    }
}