using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace NullCheckRemover.NullFixer
{
    public partial class SyntaxNullFixer
    {
        public Document Fix(BinaryExpressionSyntax binaryExpressionSyntax) 
            => binaryExpressionSyntax.Kind() switch
            {
                SyntaxKind.CoalesceExpression => FixCoalesce(binaryExpressionSyntax),
                SyntaxKind.EqualsExpression => FixEquality(binaryExpressionSyntax),
                SyntaxKind.NotEqualsExpression => FixNotEquality(binaryExpressionSyntax),
                _ => _editor.OriginalDocument
            };

        private Document FixCoalesce(BinaryExpressionSyntax binaryExpressionSyntax)
        {
            var onlyRightPart = binaryExpressionSyntax.Right;
            return ApplyFix(binaryExpressionSyntax, onlyRightPart);
        }

        private Document FixEquality(BinaryExpressionSyntax binaryExpressionSyntax) => FixComparing(SyntaxKind.TrueLiteralExpression, binaryExpressionSyntax);

        private Document FixNotEquality(BinaryExpressionSyntax binaryExpressionSyntax) => FixComparing(SyntaxKind.FalseLiteralExpression, binaryExpressionSyntax);

        private Document FixComparing(SyntaxKind literalForReplace, BinaryExpressionSyntax originalNode)
        {
            var nodeForReplace = SyntaxFactory.LiteralExpression(literalForReplace);
            return ApplyFix(originalNode, nodeForReplace);
        }
    }
}