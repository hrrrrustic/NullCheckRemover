using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullFixer
{
    public partial class SyntaxNullFixer
    {
        public Document Fix(AssignmentExpressionSyntax assignmentExpressionSyntax) 
            => assignmentExpressionSyntax.IsKind(SyntaxKind.CoalesceAssignmentExpression) ? 
                FixWithParent(assignmentExpressionSyntax.Parent, assignmentExpressionSyntax) : 
                _editor.OriginalDocument;

        private Document FixWithParent(SyntaxNode currentParent, AssignmentExpressionSyntax coalesce) 
            => currentParent switch
            {
                ParenthesizedExpressionSyntax parenthesized => FixWithParent(parenthesized.Parent, coalesce),
                StatementSyntax statement => FixCoalesceWithParentStatement(statement, coalesce),
                ExpressionSyntax expression => FixCoalesceWithParentExpression(expression, coalesce),
                { } unInterestingNode => FixWithParent(unInterestingNode.Parent, coalesce)
            };

        private Document FixCoalesceWithParentStatement(StatementSyntax statement, AssignmentExpressionSyntax coalesce) 
            => statement switch
            {
                ReturnStatementSyntax => ReplaceWithLeftPart(coalesce),
                LocalDeclarationStatementSyntax initializer => FixVariableDeclaration(initializer, coalesce),
                _ => RemoveCoalesceExpression(statement)
            };

        private Document FixCoalesceWithParentExpression(ExpressionSyntax expression, AssignmentExpressionSyntax coalesce) 
            => expression switch
            {
                AssignmentExpressionSyntax {Left: IdentifierNameSyntax} assignment => FixVariableAssignment(assignment, coalesce),
                _ => ReplaceWithLeftPart(coalesce)
            };

        private Document FixVariableAssignment(AssignmentExpressionSyntax variableAssignment, AssignmentExpressionSyntax coalesce)
        {
            if (!variableAssignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                return ReplaceWithLeftPart(coalesce);

            if (variableAssignment.Left is IdentifierNameSyntax identifier && IsSameIdentifiers(identifier.Identifier, coalesce))
                return FixWithParent(variableAssignment.Parent, coalesce);

            return ReplaceWithLeftPart(coalesce);
        }

        private Document FixVariableDeclaration(LocalDeclarationStatementSyntax declarationStatement, AssignmentExpressionSyntax coalesce)
        {
            var declaration = declarationStatement.Declaration;
            
            if(declaration.Variables.Count > 1)
                return ReplaceWithLeftPart(coalesce);

            var variable = declaration.Variables.First();

            return IsSameIdentifiers(variable.Identifier, coalesce) ?
                RemoveCoalesceExpression(declarationStatement) :
                ReplaceWithLeftPart(coalesce);
        }

        private bool IsSameIdentifiers(SyntaxToken firstIdentifier, AssignmentExpressionSyntax coalesce) 
            => SyntaxFactory.AreEquivalent(firstIdentifier, ((IdentifierNameSyntax) coalesce.Left).Identifier);

        private Document ReplaceWithLeftPart(AssignmentExpressionSyntax assignmentExpressionSyntax) 
            => ApplyFix(assignmentExpressionSyntax, assignmentExpressionSyntax.Left);

        private Document RemoveCoalesceExpression(StatementSyntax assignmentParentStatement) 
            => ApplyFix(assignmentParentStatement);
    }
}