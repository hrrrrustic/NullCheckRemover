using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullFixer
{
    public partial class SyntaxNullFixer
    {
        public Document Fix(ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax)
        {
            var leftPart = conditionalAccessExpressionSyntax.Expression.ToFullString();
            var rightPart = conditionalAccessExpressionSyntax.WhenNotNull.ToFullString();
            var withoutNullCheck = SyntaxFactory.ParseExpression(leftPart + rightPart);
            return ApplyFix(conditionalAccessExpressionSyntax, withoutNullCheck);
        }
    }
}
