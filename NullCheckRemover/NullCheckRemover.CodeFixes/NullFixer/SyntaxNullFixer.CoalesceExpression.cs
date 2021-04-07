using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullFixer
{
    public partial class SyntaxNullFixer
    {
        public Document Fix(AssignmentExpressionSyntax assignmentExpressionSyntax)
        {
            if (!assignmentExpressionSyntax.IsKind(SyntaxKind.CoalesceAssignmentExpression))
                return _editor.OriginalDocument;

            var simpleAssignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                assignmentExpressionSyntax.Left, assignmentExpressionSyntax.Right);

            return ApplyFix(assignmentExpressionSyntax, simpleAssignment);
        }
    }
}
