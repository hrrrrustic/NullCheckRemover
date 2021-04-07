using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NullCheckRemover
{
    public class NullCheckWalker : CSharpSyntaxWalker
    {
        private readonly List<Location> _diagnosticLocations = new();

        private readonly SyntaxNullAnalyzer _nullAnalyzer;

        public NullCheckWalker(SemanticModel semantic, IReadOnlyList<IParameterSymbol> parameters)
        {
            _nullAnalyzer = new SyntaxNullAnalyzer(semantic, parameters);
        }

        public IReadOnlyList<Location> GetDiagnosticLocations() => _diagnosticLocations;

        private void ConsumeAnalyzeResult(AnalyzeResult result)
        {
            if(result.NeedFix)
                _diagnosticLocations.Add(result.DiagnosticLocation!);
        }
        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            base.VisitBinaryExpression(node);

            var analyzeResult = _nullAnalyzer.Analyze(node);
            ConsumeAnalyzeResult(analyzeResult);
        }

        public override void VisitConstantPattern(ConstantPatternSyntax node)
        {
            base.VisitConstantPattern(node);

            var analyzeResult = _nullAnalyzer.Analyze(node);
            ConsumeAnalyzeResult(analyzeResult);
        }

        public override void VisitRecursivePattern(RecursivePatternSyntax node)
        {
            base.VisitRecursivePattern(node);

            var analyzeResult = _nullAnalyzer.Analyze(node);
            ConsumeAnalyzeResult(analyzeResult);
        }

        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            base.VisitConditionalAccessExpression(node);

            var analyzeResult = _nullAnalyzer.Analyze(node);
            ConsumeAnalyzeResult(analyzeResult);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            base.VisitAssignmentExpression(node);

            var analyzeResult = _nullAnalyzer.Analyze(node);
            ConsumeAnalyzeResult(analyzeResult);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            base.VisitSwitchStatement(node);

            var analyzeResult = _nullAnalyzer.Analyze(node);
            ConsumeAnalyzeResult(analyzeResult);
        }
    }
}