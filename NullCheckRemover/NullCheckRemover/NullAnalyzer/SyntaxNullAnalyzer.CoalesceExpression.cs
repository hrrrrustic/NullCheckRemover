using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.SyntaxNullAnalyzer
{
    public partial class SyntaxNullAnalyzer
    {
        public AnalyzeResult Analyze(AssignmentExpressionSyntax assignmentExpressionSyntax)
        {
            if (!assignmentExpressionSyntax.IsKind(SyntaxKind.CoalesceAssignmentExpression))
                return AnalyzeResult.False();

            return AnalyzeOperand(assignmentExpressionSyntax.Left, assignmentExpressionSyntax);
        }
    }
}
