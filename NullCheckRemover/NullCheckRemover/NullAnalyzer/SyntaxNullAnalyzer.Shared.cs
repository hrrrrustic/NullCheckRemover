using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.SyntaxNullAnalyzer
{
    public partial class SyntaxNullAnalyzer
    {
        private readonly SemanticModel _semantic;
        private readonly IReadOnlyList<IParameterSymbol> _parameters;
        public SyntaxNullAnalyzer(SemanticModel semantic, IReadOnlyList<IParameterSymbol> parameters)
        {
            _semantic = semantic;
            _parameters = parameters;
        }

        private bool IsInterestingForAnalyze(ISymbol? symbol) 
            => _parameters.Any(k => SymbolEqualityComparer.Default.Equals(k, symbol));

        private bool IsInterestingForAnalyze(ExpressionSyntax expression)
        {
            var symbol = _semantic.GetSymbolInfo(expression).Symbol;
            return IsInterestingForAnalyze(symbol);
        }

        private AnalyzeResult AnalyzeOperand(ExpressionSyntax operand, ExpressionSyntax nodeForLocationOnSuccess) 
            => IsInterestingForAnalyze(operand) ? 
                AnalyzeResult.True(nodeForLocationOnSuccess.GetLocation()) : 
                AnalyzeResult.False();
    }
}