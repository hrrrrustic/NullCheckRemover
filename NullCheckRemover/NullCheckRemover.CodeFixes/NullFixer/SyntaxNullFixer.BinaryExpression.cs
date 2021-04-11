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
            return ReplaceNode(binaryExpressionSyntax, onlyRightPart);
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
            return ReplaceNode(originalNode, nodeForReplace);
        }

        private Document FixWithBlockSimplifying(BinaryExpressionSyntax binaryExpressionSyntax, SyntaxKind literalForReplace)
        {
            if (binaryExpressionSyntax.Parent is IfStatementSyntax ifStatement)
                return InlineIf(ifStatement, literalForReplace);

            if (binaryExpressionSyntax.Parent is ConditionalExpressionSyntax conditional)
                return InlineConditional(conditional, literalForReplace);

            return FixComparing(literalForReplace, binaryExpressionSyntax);
        }

        private Document InlineConditional(ConditionalExpressionSyntax conditional, SyntaxKind comparingExpression) 
            => comparingExpression switch
            {
                SyntaxKind.EqualsExpression => InlineConditionalPart(conditional, false),
                SyntaxKind.NotEqualsExpression => InlineConditionalPart(conditional, true)
            };

        private Document InlineConditionalPart(ConditionalExpressionSyntax conditional, bool conditionValue)
        {
            var nodeForInline = conditionValue ? conditional.WhenTrue : conditional.WhenFalse;
            return ReplaceNode(conditional, nodeForInline);
        }

        private Document InlineIf(IfStatementSyntax ifStatement, SyntaxKind comparingExpression) 
            => (ifStatement.Else is null, comparingExpression) switch
            {
                (true, SyntaxKind.EqualsExpression) => RemoveNode(ifStatement),
                (true, SyntaxKind.NotEqualsExpression) => InlineIf(ifStatement),
                (false, SyntaxKind.EqualsExpression) => InlineElse(ifStatement),
                (false, SyntaxKind.NotEqualsExpression) => InlineIf(ifStatement),
            };

        private Document InlineElse(IfStatementSyntax ifStatement)
        {
            var elseNode = ifStatement.Else;
            if (elseNode!.Statement is IfStatementSyntax elseIf)
                return ReplaceNode(ifStatement, elseIf);

            return InlineNodes(ifStatement, elseNode.Statement.ChildNodes());
        }

        private Document InlineIf(IfStatementSyntax ifStatement) => InlineNodes(ifStatement, ifStatement.Statement.ChildNodes());

        private Document FixComplexBinaryExpression(BinaryExpressionSyntax node, BinaryExpressionSyntax parentBinary) 
            => (node.Kind(), parentBinary.Kind()) switch
            {
                (SyntaxKind.EqualsExpression, SyntaxKind.LogicalAndExpression) => FixComparing(SyntaxKind.FalseLiteralExpression, node),
                (SyntaxKind.NotEqualsExpression, SyntaxKind.LogicalAndExpression) => ReplaceNode(parentBinary, parentBinary.Right),
                (SyntaxKind.EqualsExpression, SyntaxKind.LogicalOrExpression) => ReplaceNode(parentBinary, parentBinary.Right),
                (SyntaxKind.NotEqualsExpression, SyntaxKind.LogicalOrExpression) => FixComparing(SyntaxKind.TrueLiteralExpression, node),
                _ => _editor.OriginalDocument
            };
    }
}