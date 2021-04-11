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
                SyntaxKind.EqualsExpression => FixComparing(binaryExpressionSyntax, SyntaxKind.EqualsExpression),
                SyntaxKind.NotEqualsExpression => FixComparing(binaryExpressionSyntax, SyntaxKind.NotEqualsExpression),
                _ => _editor.OriginalDocument
            };

        private Document FixCoalesce(BinaryExpressionSyntax binaryExpressionSyntax)
        {
            var onlyRightPart = binaryExpressionSyntax.Right;
            return ApplyFix(binaryExpressionSyntax, onlyRightPart);
        }

        private Document FixComparing(BinaryExpressionSyntax binaryExpressionSyntax, SyntaxKind comparingExpression)
        {
            if (binaryExpressionSyntax.Parent is BinaryExpressionSyntax alsoBinary)
                return FixComplexBinaryExpression(binaryExpressionSyntax, alsoBinary);

            return FixWithBlockSimplifying(binaryExpressionSyntax, comparingExpression);
        }

        private Document FixEquality(BinaryExpressionSyntax binaryExpressionSyntax) 
            => FixComparing(SyntaxKind.FalseLiteralExpression, binaryExpressionSyntax);

        private Document FixNotEquality(BinaryExpressionSyntax binaryExpressionSyntax) 
            => FixComparing(SyntaxKind.TrueLiteralExpression, binaryExpressionSyntax);

        private Document FixComparing(SyntaxKind literalForReplace, BinaryExpressionSyntax originalNode)
        {
            var nodeForReplace = SyntaxFactory.LiteralExpression(literalForReplace);
            return ApplyFix(originalNode, nodeForReplace);
        }

        private Document FixWithBlockSimplifying(BinaryExpressionSyntax binaryExpressionSyntax, SyntaxKind literalForReplace)
        {
            if (binaryExpressionSyntax.Parent is IfStatementSyntax ifStatement)
                return InlineIf(ifStatement, literalForReplace);

            return FixComparing(literalForReplace, binaryExpressionSyntax);
        }

        private Document InlineIf(IfStatementSyntax ifStatement, SyntaxKind comparingExpression)
        {
            return (ifStatement.Else is null, comparingExpression) switch
            {
                (true, SyntaxKind.EqualsExpression) => ApplyFix(ifStatement),
                (true, SyntaxKind.NotEqualsExpression) => InlineIf(ifStatement),
                (false, SyntaxKind.EqualsExpression) => InlineElse(ifStatement),
                (false, SyntaxKind.NotEqualsExpression) => InlineIf(ifStatement),
            };
        }

        private Document InlineElse(IfStatementSyntax ifStatement)
        {
            var elseNode = ifStatement.Else;
            if (elseNode!.Statement is IfStatementSyntax elseIf)
                return ApplyFix(ifStatement, elseIf);

            return InlineNodes(ifStatement, elseNode.Statement.ChildNodes());
        }

        private Document InlineIf(IfStatementSyntax ifStatement) => InlineNodes(ifStatement, ifStatement.Statement.ChildNodes());

        private Document FixComplexBinaryExpression(BinaryExpressionSyntax node, BinaryExpressionSyntax parentBinary)
        {
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