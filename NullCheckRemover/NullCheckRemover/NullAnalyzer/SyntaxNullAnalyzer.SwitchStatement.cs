using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullAnalyzer
{
    public partial class SyntaxNullAnalyzer
    {
        public AnalyzeResult Analyze(SwitchStatementSyntax switchStatement)
        {
            if (!IsInterestingForAnalyze(switchStatement.Expression))
                return AnalyzeResult.False();

            var nullLabel = switchStatement
                .Sections
                .SelectMany(k => k.Labels)
                .SingleOrDefault(k => k is CaseSwitchLabelSyntax label && label.Value.IsKind(SyntaxKind.NullLiteralExpression));

            return nullLabel is null ? AnalyzeResult.False() : AnalyzeResult.True(nullLabel.GetLocation());
        }
    }
}