using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullFixer
{
    public partial class SyntaxNullFixer
    {
        public Document Fix(BinaryExpressionSyntax binaryExpressionSyntax) 
            => binaryExpressionSyntax.Kind() switch
            {
                SyntaxKind.CoalesceExpression => FixCoalesce(binaryExpressionSyntax),
                SyntaxKind.EqualsExpression => FixComplexBinaryExpression(binaryExpressionSyntax),
                SyntaxKind.NotEqualsExpression => FixComplexBinaryExpression(binaryExpressionSyntax),
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

        private Document FixComplexBinaryExpression(BinaryExpressionSyntax node)
        {
            if (node.Parent is not BinaryExpressionSyntax parentBinary)
                return _editor.OriginalDocument;

            return (node.Kind(), parentBinary.Kind()) switch
            {
                (SyntaxKind.EqualsExpression, SyntaxKind.LogicalAndExpression) => _editor.OriginalDocument,
                (SyntaxKind.NotEqualsExpression, SyntaxKind.LogicalAndExpression) => ApplyFix(parentBinary, parentBinary.Right),
                (SyntaxKind.EqualsExpression, SyntaxKind.LogicalOrExpression) => ApplyFix(parentBinary, parentBinary.Right),
                (SyntaxKind.NotEqualsExpression, SyntaxKind.LogicalOrExpression) => _editor.OriginalDocument,
                _ => _editor.OriginalDocument
            };
        }
    }
}