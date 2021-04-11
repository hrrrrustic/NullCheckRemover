using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullAnalyzer
{
    public partial class SyntaxNullAnalyzer
    {
        public AnalyzeResult Analyze(RecursivePatternSyntax recursivePatternSyntax)
        {
            if (!IsInterestingPatternMatching(recursivePatternSyntax))
                return AnalyzeResult.False();

            if (recursivePatternSyntax.PositionalPatternClause?.Subpatterns.Any() ?? false)
                return AnalyzeResult.False();

            if (recursivePatternSyntax.PropertyPatternClause?.Subpatterns.Any() ?? false)
                return AnalyzeResult.False();

            return AnalyzeResult.True(recursivePatternSyntax.GetLocation());
        }

        public AnalyzeResult Analyze(ConstantPatternSyntax constantPatternSyntax)
        {
            if (!IsInterestingPatternMatching(constantPatternSyntax))
                return AnalyzeResult.False();

            if (!constantPatternSyntax.Expression.IsKind(SyntaxKind.NullLiteralExpression))
                return AnalyzeResult.False();

            return AnalyzeResult.True(constantPatternSyntax.GetLocation());
        }

        private bool IsInterestingPatternMatching(PatternSyntax patternSyntax)
        {
            var operand = GetPatternMatchingOperand(patternSyntax);
            return IsInterestingForAnalyze(operand);
        }

        private ExpressionSyntax GetPatternMatchingOperand(PatternSyntax patternSyntax)
        {
            return GetOperand(patternSyntax);

            // Вынес, чтобы сам вход в метод был немного типизированнее
            static ExpressionSyntax GetOperand(SyntaxNode node) 
                => node switch
                {
                    IsPatternExpressionSyntax isPattern => isPattern.Expression,
                    SwitchExpressionSyntax switchExpression => switchExpression.GoverningExpression,
                    { } unInterestingNode => GetOperand(unInterestingNode.Parent),
                    null => throw new NotSupportedException("Паттерн матчинг не внутри свитча/is паттерна не реализован")
                };
        }
    }
}