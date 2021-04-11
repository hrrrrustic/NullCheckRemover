using System;
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

        public Func<AnalyzeResult> GetAnalyzerFor<T>(T? node) where T : SyntaxNode
            => node switch
            {
                SwitchStatementSyntax switchStatement => () => Analyze(switchStatement),
                BinaryExpressionSyntax binaryExpression => () => Analyze(binaryExpression),
                ConditionalAccessExpressionSyntax conditionalAccess => () => Analyze(conditionalAccess),
                AssignmentExpressionSyntax assignmentExpression => () => Analyze(assignmentExpression),
                RecursivePatternSyntax recursivePattern => () => Analyze(recursivePattern),
                ConstantPatternSyntax constantPattern => () => Analyze(constantPattern),
                _ => AnalyzeResult.False
            };

        private bool IsInterestingForAnalyze(ISymbol? symbol) 
            => _parameters.Any(k => IsEqualsSymbol(k, symbol) || UnsafeIsEqualsSymbolByName(k, symbol));

        private bool IsEqualsSymbol(ISymbol? first, ISymbol? second) => SymbolEqualityComparer.Default.Equals(first, second);

        private bool UnsafeIsEqualsSymbolByName(ISymbol? first, ISymbol? second) => first?.Name == second?.Name;

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