using System.Linq;
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
                SyntaxKind.EqualsExpression => FixComparing(binaryExpressionSyntax, false),
                SyntaxKind.NotEqualsExpression => FixComparing(binaryExpressionSyntax, true),
                _ => _editor.OriginalDocument
            };

        private Document FixCoalesce(BinaryExpressionSyntax binaryExpressionSyntax)
        {
            var onlyRightPart = binaryExpressionSyntax.Right;
            return ReplaceNode(binaryExpressionSyntax, onlyRightPart);
        }

        private Document FixComparing(BinaryExpressionSyntax binaryExpressionSyntax, bool expressionResultAfterFix)
        {
            if (binaryExpressionSyntax.Parent is BinaryExpressionSyntax alsoBinary)
                return FixComplexBinaryExpression(binaryExpressionSyntax, alsoBinary);

            return FixWithBlockSimplifying(binaryExpressionSyntax, expressionResultAfterFix);
        }

        private Document FixComparing(bool expressionResultAfterFix, BinaryExpressionSyntax originalNode)
        {
            var literalKind = expressionResultAfterFix ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression;
            var nodeForReplace = SyntaxFactory.LiteralExpression(literalKind);
            return ReplaceNode(originalNode, nodeForReplace);
        }

        private Document FixWithBlockSimplifying(BinaryExpressionSyntax binaryExpressionSyntax, bool expressionResultAfterFix)
        {
            if (binaryExpressionSyntax.Parent is IfStatementSyntax ifStatement)
                return InlineIf(ifStatement, expressionResultAfterFix);

            if (binaryExpressionSyntax.Parent is ConditionalExpressionSyntax conditional)
                return InlineConditional(conditional, expressionResultAfterFix);

            return FixComparing(expressionResultAfterFix, binaryExpressionSyntax);
        }

        private Document InlineConditional(ConditionalExpressionSyntax conditional, bool conditionValue)
        {
            var nodeForInline = conditionValue ? conditional.WhenTrue : conditional.WhenFalse;
            return ReplaceNode(conditional, nodeForInline);
        }

        private Document InlineIf(IfStatementSyntax ifStatement, bool ifExpressionValue) 
            => (ifStatement.Else is null, ifExpressionValue) switch
            {
                (true, false) => RemoveNode(ifStatement),
                (true, true) => InlineIf(ifStatement),
                (false, false) => InlineElse(ifStatement),
                (false, true) => InlineIf(ifStatement),
            };

        private Document InlineElse(IfStatementSyntax ifStatement)
        {
            var elseNode = ifStatement.Else;
            if (elseNode!.Statement is IfStatementSyntax elseIf)
                return ReplaceNode(ifStatement, elseIf);

            if (ifStatement.Parent is ElseClauseSyntax parentElse)
                return InlineElse(parentElse.Ancestors().OfType<IfStatementSyntax>().First());

            return InlineNodes(ifStatement, elseNode.Statement.ChildNodes());
        }

        private Document InlineIf(IfStatementSyntax ifStatement) => InlineNodes(ifStatement, ifStatement.Statement.ChildNodes());

        private Document FixComplexBinaryExpression(BinaryExpressionSyntax node, BinaryExpressionSyntax parentBinary) 
            => (node.Kind(), parentBinary.Kind()) switch
            {
                (SyntaxKind.EqualsExpression, SyntaxKind.LogicalAndExpression) => FixComparing(false, node),
                (SyntaxKind.NotEqualsExpression, SyntaxKind.LogicalAndExpression) => ReplaceNode(parentBinary, parentBinary.Right),
                (SyntaxKind.EqualsExpression, SyntaxKind.LogicalOrExpression) => ReplaceNode(parentBinary, parentBinary.Right),
                (SyntaxKind.NotEqualsExpression, SyntaxKind.LogicalOrExpression) => FixComparing(true, node),
                _ => _editor.OriginalDocument
            };
    }
}