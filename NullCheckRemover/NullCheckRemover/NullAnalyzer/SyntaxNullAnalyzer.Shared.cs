using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullAnalyzer
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

        private AnalyzeResult AnalyzeOperand(ExpressionSyntax operand, SyntaxNode nodeForLocationOnSuccess)
            => AnalyzeOperandPrivate(operand, nodeForLocationOnSuccess.GetLocation());

        private AnalyzeResult AnalyzeOperand(ExpressionSyntax operand, SyntaxToken tokenForLocationOnSuccess)
            => AnalyzeOperandPrivate(operand, tokenForLocationOnSuccess.GetLocation());

        private AnalyzeResult AnalyzeOperandPrivate(ExpressionSyntax operand, Location location)
            => IsInterestingForAnalyze(operand) ? 
                AnalyzeResult.True(location) : 
                AnalyzeResult.False();
    }
}