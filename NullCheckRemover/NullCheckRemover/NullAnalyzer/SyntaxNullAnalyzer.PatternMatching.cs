using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.SyntaxNullAnalyzer
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

        private ExpressionSyntax GetPatternMatchingOperand<T>(T patternSyntax) where T : PatternSyntax
        {
            SyntaxNode? current = patternSyntax;

            while (true)
            {
                switch (current)
                {
                    case IsPatternExpressionSyntax isPattern:
                        return isPattern.Expression;
                    case SwitchExpressionSyntax switchExpression:
                        return switchExpression.GoverningExpression;
                    case null:
                        throw new NotSupportedException();
                    default:
                        current = current.Parent;
                        break;
                }
            }
        }
    }
}
