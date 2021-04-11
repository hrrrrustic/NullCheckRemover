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
                .SingleOrDefault(k =>
                {
                    //Я без понятия почему у SwitchLabelSyntax нету проперти Value >_>
                    //Его просто нету @_@ В дебаге вижу его, но из кода вызвать не могу. На гитахбе в сурсах тоже его не нашел
                    //Хотя в документации оно везде есть
                    var childs = k.ChildNodes().OfType<LiteralExpressionSyntax>().FirstOrDefault();
                    return childs?.IsKind(SyntaxKind.NullLiteralExpression) ?? false;
                });

            return nullLabel is null ? AnalyzeResult.False() : AnalyzeResult.True(nullLabel.GetLocation());
        }
    }
}