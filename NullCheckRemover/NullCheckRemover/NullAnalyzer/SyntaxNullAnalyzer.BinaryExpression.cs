using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.SyntaxNullAnalyzer
{
    public partial class SyntaxNullAnalyzer
    {
        public AnalyzeResult Analyze(BinaryExpressionSyntax binaryExpression)
            => binaryExpression.Kind() switch
            {
                SyntaxKind.CoalesceExpression => AnalyzeCoalesce(binaryExpression),
                SyntaxKind.NotEqualsExpression or SyntaxKind.EqualsExpression => AnalyzeEqualityCompare(binaryExpression),
                _ => AnalyzeResult.False()
            };

        private AnalyzeResult AnalyzeCoalesce(BinaryExpressionSyntax binaryExpressionSyntax)
            => AnalyzeOperand(binaryExpressionSyntax.Left, binaryExpressionSyntax);

        private AnalyzeResult AnalyzeEqualityCompare(BinaryExpressionSyntax binaryExpressionSyntax)
        {
            var leftKind = binaryExpressionSyntax.Left.Kind();

            var rightKind = binaryExpressionSyntax.Right.Kind();
            var leftIsInteresting = leftKind is SyntaxKind.DefaultLiteralExpression or SyntaxKind.NullLiteralExpression;
            var rightIsInteresting = rightKind is SyntaxKind.DefaultLiteralExpression or SyntaxKind.NullLiteralExpression;

            if (!leftIsInteresting && !rightIsInteresting)
                return AnalyzeResult.False();

            var nodeForSearching = leftIsInteresting ? binaryExpressionSyntax.Right : binaryExpressionSyntax.Left;

            return AnalyzeOperand(nodeForSearching, binaryExpressionSyntax);
        }

    }
}
