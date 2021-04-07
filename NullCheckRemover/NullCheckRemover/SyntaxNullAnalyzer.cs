using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover
{
    public class SyntaxNullAnalyzer
    {
        private readonly SemanticModel _semantic;

        private readonly IReadOnlyList<IParameterSymbol> _parameters;
        public SyntaxNullAnalyzer(SemanticModel semantic, IReadOnlyList<IParameterSymbol> parameters)
        {
            _semantic = semantic;
            _parameters = parameters;
        }

        private bool IsInterestingForAnalyze(ISymbol? symbol) 
            => _parameters.Any(k => SymbolEqualityComparer.Default.Equals(k, symbol));

        private bool IsInterestingForAnalyze(ExpressionSyntax expression)
        {
            var symbol = _semantic.GetSymbolInfo(expression).Symbol;
            return IsInterestingForAnalyze(symbol);
        }

        private AnalyzeResult AnalyzeOperand(ExpressionSyntax operand, ExpressionSyntax nodeForLocationOnSuccess) 
            => IsInterestingForAnalyze(operand) ? 
                AnalyzeResult.True(nodeForLocationOnSuccess.GetLocation()) : 
                AnalyzeResult.False();

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
                    var childs = k.ChildNodes().OfType<LiteralExpressionSyntax>().ToList();
                    return childs.Count == 1 && childs[0].IsKind(SyntaxKind.NullLiteralExpression);
                });

            return nullLabel is null ? AnalyzeResult.False() : AnalyzeResult.True(nullLabel.GetLocation());
        }

        public AnalyzeResult Analyze(BinaryExpressionSyntax binaryExpression) 
            => binaryExpression.Kind() switch
            {
                SyntaxKind.CoalesceExpression => AnalyzeCoalesce(binaryExpression),
                SyntaxKind.NotEqualsExpression or SyntaxKind.EqualsExpression => AnalyzeEqualityCompare(binaryExpression),
                _ => AnalyzeResult.False()
            };

        private AnalyzeResult AnalyzeCoalesce(BinaryExpressionSyntax binaryExpressionSyntax) 
            => AnalyzeOperand(binaryExpressionSyntax.Left, binaryExpressionSyntax);

        private AnalyzeResult AnalyzeEqualityCompare(BinaryExpressionSyntax binaryExpressionSyntax)
        {
            var leftKind = binaryExpressionSyntax.Left.Kind();

            var rightKind = binaryExpressionSyntax.Right.Kind();
            var leftIsInteresting = leftKind is SyntaxKind.DefaultLiteralExpression or SyntaxKind.NullLiteralExpression;
            var rightIsInteresting = rightKind is SyntaxKind.DefaultLiteralExpression or SyntaxKind.NullLiteralExpression;

            if (!leftIsInteresting && !rightIsInteresting)
                return AnalyzeResult.False();

            var nodeForSearching = leftIsInteresting ? binaryExpressionSyntax.Right : binaryExpressionSyntax.Left;

            return AnalyzeOperand(nodeForSearching, binaryExpressionSyntax);
        }

        public AnalyzeResult Analyze(RecursivePatternSyntax recursivePatternSyntax)
        {
            if (!IsInterestingPatternMatching(recursivePatternSyntax))
                return AnalyzeResult.False();

            if (recursivePatternSyntax.PositionalPatternClause?.Subpatterns.Any() ?? false)
                return AnalyzeResult.False();

            if (recursivePatternSyntax.PropertyPatternClause?.Subpatterns.Any() ?? false)
                return AnalyzeResult.False();

            return AnalyzeResult.True(recursivePatternSyntax.GetLocation());
        }

        public AnalyzeResult Analyze(ConstantPatternSyntax constantPatternSyntax)
        {
            if (!IsInterestingPatternMatching(constantPatternSyntax))
                return AnalyzeResult.False();

            if (!constantPatternSyntax.Expression.IsKind(SyntaxKind.NullLiteralExpression))
                return AnalyzeResult.False();

            return AnalyzeResult.True(constantPatternSyntax.GetLocation());
        }

        public AnalyzeResult Analyze(ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax) 
            => AnalyzeOperand(conditionalAccessExpressionSyntax.Expression, conditionalAccessExpressionSyntax);

        public AnalyzeResult Analyze(AssignmentExpressionSyntax assignmentExpressionSyntax)
        {
            if (!assignmentExpressionSyntax.IsKind(SyntaxKind.CoalesceAssignmentExpression))
                return AnalyzeResult.False();

            return AnalyzeOperand(assignmentExpressionSyntax.Left, assignmentExpressionSyntax);
        }

        private bool IsInterestingPatternMatching(PatternSyntax patternSyntax)
        {
            var operand = GetPatternMatchingOperand(patternSyntax);
            return IsInterestingForAnalyze(operand);
        }
        private ExpressionSyntax GetPatternMatchingOperand<T>(T patternSyntax) where T : PatternSyntax
        {
            SyntaxNode? current = patternSyntax;

            while (true)
            {
                switch (current)
                {
                    case IsPatternExpressionSyntax isPattern:
                        return isPattern.Expression;
                    case SwitchExpressionSyntax switchExpression:
                        return switchExpression.GoverningExpression;
                    case null:
                        throw new NotSupportedException();
                    default:
                        current = current.Parent;
                        break;
                }
            }
        }
    }
}