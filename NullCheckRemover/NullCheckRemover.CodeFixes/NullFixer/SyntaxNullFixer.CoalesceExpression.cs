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

        // Ниже анализ используется ли возвращаемое значение оператора ??= (и кем).
        // В зависимости от результата могу вообще удалить все, а могу оставить только левую часть
        private Document FixWithParent(SyntaxNode? currentParent, AssignmentExpressionSyntax coalesce) 
            => currentParent switch
            {
                ParenthesizedExpressionSyntax parenthesized => FixWithParent(parenthesized.Parent, coalesce),
                StatementSyntax statement => FixCoalesceWithParentStatement(statement, coalesce),
                ExpressionSyntax expression => FixCoalesceWithParentExpression(expression, coalesce),
                { } unInterestingNode => FixWithParent(unInterestingNode.Parent, coalesce),
                null => ReplaceWithLeftPart(coalesce)
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

            // Анализ каких-то совсем стремных конструкций типа args = args ??= new()
            // В таком случае можно полностью удалить строку. А вот если будет var x = args ??= new()
            // Тогда надо оставлять левую часть ??=
            return IsSameIdentifiers(variable.Identifier, coalesce) ?
                RemoveCoalesceExpression(declarationStatement) :
                ReplaceWithLeftPart(coalesce);
        }

        private bool IsSameIdentifiers(SyntaxToken firstIdentifier, AssignmentExpressionSyntax coalesce) 
            => SyntaxFactory.AreEquivalent(firstIdentifier, ((IdentifierNameSyntax) coalesce.Left).Identifier);

        private Document ReplaceWithLeftPart(AssignmentExpressionSyntax assignmentExpressionSyntax) 
            => ReplaceNode(assignmentExpressionSyntax, assignmentExpressionSyntax.Left);

        private Document RemoveCoalesceExpression(StatementSyntax assignmentParentStatement) 
            => RemoveNode(assignmentParentStatement);
    }
}