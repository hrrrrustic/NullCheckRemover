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
        private readonly List<Location> _diagnosticLocations = new List<Location>();

        public NullCheckWalker(SemanticModel semantic, IReadOnlyList<IParameterSymbol> parameters)
        {
            _semantic = semantic;
            _parameters = parameters;
        }

        public IReadOnlyList<Location> GetDiagnosticLocations() => _diagnosticLocations;

        private bool IsParameter(ISymbol symbol)
        {
            return _parameters.Any(k => SymbolEqualityComparer.Default.Equals(k, symbol));
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            base.VisitBinaryExpression(node);

            if (node.IsKind(SyntaxKind.CoalesceExpression))
            {
                CheckCoalesceExpression(node);
                return;
            }

            if (node.IsKind(SyntaxKind.EqualsExpression))
            {
                CheckEqualityExpression(node);
                return;
            }

            if (node.IsKind(SyntaxKind.NotEqualsExpression))
            {
                CheckEqualityExpression(node);
                return;
            }
        }

        public override void VisitIsPatternExpression(IsPatternExpressionSyntax node)
        {
            base.VisitIsPatternExpression(node);

            if(!IsParameter(_semantic.GetSymbolInfo(node.Expression).Symbol))
                return;

            if (node.Pattern is ConstantPatternSyntax constant)
            {
                CheckIsNullPattern(constant);
                return;
            }

            if (node.Pattern is RecursivePatternSyntax recursive)
            {
                CheckIsSomethingPattern(recursive);
                return;
            }
        }
        
        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            base.VisitConditionalAccessExpression(node);

            if (!IsParameter(_semantic.GetSymbolInfo(node.Expression).Symbol))
                return;

            _diagnosticLocations.Add(node.GetLocation());
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if(!node.IsKind(SyntaxKind.CoalesceAssignmentExpression))
                return;

            if(!IsParameter(_semantic.GetSymbolInfo(node.Left).Symbol))
                return;

            _diagnosticLocations.Add(node.GetLocation());
        }

        private void CheckCoalesceExpression(BinaryExpressionSyntax binary)
        {
            if(!IsParameter(_semantic.GetSymbolInfo(binary.Left).Symbol))
                return;

            _diagnosticLocations.Add(binary.GetLocation());
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

            _diagnosticLocations.Add(binary.GetLocation());
        }

        private void CheckIsNullPattern(ConstantPatternSyntax constant)
        {
            if (!constant.Expression.IsKind(SyntaxKind.NullLiteralExpression))
                return;

            _diagnosticLocations.Add(constant.GetLocation());
        }

        private void CheckIsSomethingPattern(RecursivePatternSyntax recursivePattern)
        {
            if(recursivePattern.PositionalPatternClause.Subpatterns.Any())
                return;

            if(recursivePattern.PropertyPatternClause.Subpatterns.Any())
                return;

            _diagnosticLocations.Add(recursivePattern.GetLocation());
        }
    }
}