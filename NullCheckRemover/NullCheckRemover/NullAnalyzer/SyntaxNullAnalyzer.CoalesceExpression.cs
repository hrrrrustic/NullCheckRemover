using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullAnalyzer
{
    public partial class SyntaxNullAnalyzer
    {
        public AnalyzeResult Analyze(AssignmentExpressionSyntax assignmentExpressionSyntax) 
            => assignmentExpressionSyntax.IsKind(SyntaxKind.CoalesceAssignmentExpression) ? 
                AnalyzeOperand(assignmentExpressionSyntax.Left, assignmentExpressionSyntax.OperatorToken) : 
                AnalyzeResult.False();
    }
}