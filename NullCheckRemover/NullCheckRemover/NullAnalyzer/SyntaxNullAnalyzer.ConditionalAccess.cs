using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullAnalyzer
{
    public partial class SyntaxNullAnalyzer
    {
        public AnalyzeResult Analyze(ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax)
            => AnalyzeOperand(conditionalAccessExpressionSyntax.Expression, conditionalAccessExpressionSyntax);
    }
}
