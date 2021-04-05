using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover
{
    public class NullCheckWalker : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semantic;
        private readonly IReadOnlyList<IParameterSymbol> _parameters;
        private readonly List<Location> _diagnosticLocations = new();

        public NullCheckWalker(SemanticModel semantic, IReadOnlyList<IParameterSymbol> parameters)
        {
            _semantic = semantic;
            _parameters = parameters;
        }

        public IReadOnlyList<Location> GetDiagnosticLocations() => _diagnosticLocations;

        private bool IsParameter(ISymbol? symbol) => _parameters.Any(k => SymbolEqualityComparer.Default.Equals(k, symbol));
        private void AddDiagnostic(SyntaxNode node) => _diagnosticLocations.Add(node.GetLocation());

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            base.VisitBinaryExpression(node);

            switch (node.Kind())
            {
                case SyntaxKind.CoalesceExpression:
                    CheckCoalesceExpression(node);
                    return;
                case SyntaxKind.NotEqualsExpression:
                case SyntaxKind.EqualsExpression:
                    CheckEqualityExpression(node);
                    return;
                default:
                    return;
            }
        }

        public override void VisitIsPatternExpression(IsPatternExpressionSyntax node)
        {
            base.VisitIsPatternExpression(node);

            if(!IsParameter(_semantic.GetSymbolInfo(node.Expression).Symbol))
                return;

            switch (node.Pattern)
            {
                case ConstantPatternSyntax constant:
                    CheckIsNullPattern(constant);
                    break;
                case RecursivePatternSyntax recursive:
                    CheckIsSomethingPattern(recursive);
                    break;
                case UnaryPatternSyntax notPattern:
                    CheckIsNotNullPattern(notPattern);
                    break;
            }
        }
        
        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            base.VisitConditionalAccessExpression(node);

            if (!IsParameter(_semantic.GetSymbolInfo(node.Expression).Symbol))
                return;

            AddDiagnostic(node);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if(!node.IsKind(SyntaxKind.CoalesceAssignmentExpression))
                return;

            if(!IsParameter(_semantic.GetSymbolInfo(node.Left).Symbol))
                return;

            AddDiagnostic(node);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            base.VisitSwitchStatement(node);

            if (!IsParameter(_semantic.GetSymbolInfo(node.Expression).Symbol))
                return;

            var nullLabel = node
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

            if(nullLabel is null)
                return;

            AddDiagnostic(nullLabel);
        }

        public override void VisitSwitchExpression(SwitchExpressionSyntax node)
        {
            base.VisitSwitchExpression(node);

            if (!IsParameter(_semantic.GetSymbolInfo(node.GoverningExpression).Symbol))
                return;

            var nullArm = node
                .Arms
                .SingleOrDefault(k => k.Pattern is ConstantPatternSyntax constant && constant.Expression.IsKind(SyntaxKind.NullLiteralExpression));

            if(nullArm is null)
                return;

            AddDiagnostic(nullArm);
        }

        private void CheckCoalesceExpression(BinaryExpressionSyntax binary)
        {
            if(!IsParameter(_semantic.GetSymbolInfo(binary.Left).Symbol))
                return;

            AddDiagnostic(binary);
        }

        private void CheckEqualityExpression(BinaryExpressionSyntax binary)
        {
            var leftKind = binary.Left.Kind();
            var rightKind = binary.Right.Kind();
            var leftIsInteresting = leftKind is SyntaxKind.DefaultLiteralExpression or SyntaxKind.NullLiteralExpression;
            var rightIsInteresting = rightKind is SyntaxKind.DefaultLiteralExpression or SyntaxKind.NullLiteralExpression;
            if (!leftIsInteresting && !rightIsInteresting)
                return;

            var nodeForSearching = leftIsInteresting ? binary.Right : binary.Left;
            var symbol = _semantic.GetSymbolInfo(nodeForSearching).Symbol;
            if (symbol is not IParameterSymbol parameter)
                return;

            if (!IsParameter(parameter))
                return;

            AddDiagnostic(binary);
        }

        private void CheckIsNullPattern(ConstantPatternSyntax constant)
        {
            if (!constant.Expression.IsKind(SyntaxKind.NullLiteralExpression))
                return;

            AddDiagnostic(constant);
        }

        private void CheckIsSomethingPattern(RecursivePatternSyntax recursivePattern)
        {
            if(recursivePattern.PositionalPatternClause?.Subpatterns.Any() ?? false)
                return;

            if(recursivePattern.PropertyPatternClause?.Subpatterns.Any() ?? false)
                return;

            AddDiagnostic(recursivePattern);
        }

        private void CheckIsNotNullPattern(UnaryPatternSyntax notPattern)
        {
            if(notPattern.Pattern is not ConstantPatternSyntax constant)
                return;

            CheckIsNullPattern(constant);
        }
    }
}