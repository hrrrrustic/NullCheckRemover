using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.SyntaxNullAnalyzer
{
    public partial class SyntaxNullAnalyzer
    {
        public AnalyzeResult Analyze(ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax)
            => AnalyzeOperand(conditionalAccessExpressionSyntax.Expression, conditionalAccessExpressionSyntax);
    }
}
