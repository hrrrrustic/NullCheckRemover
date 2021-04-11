using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullFixer
{
    public partial class SyntaxNullFixer
    {
        // Ну, это прям совсем ненедажено, нетипизированно, непроизводительно и тд
        // Но для такого с виду простого фикса дерево трансформируется довольно сильно
        // И сбилдить ноду для реплейса чет не получилось - пришлось читерить :(
        public Document Fix(ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax)
        {
            var leftPart = conditionalAccessExpressionSyntax.Expression.ToFullString();
            var rightPart = conditionalAccessExpressionSyntax.WhenNotNull.ToFullString();
            var withoutNullCheck = SyntaxFactory.ParseExpression(leftPart + rightPart);
            return ReplaceNode(conditionalAccessExpressionSyntax, withoutNullCheck);
        }
    }
}
