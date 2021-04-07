using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NullCheckRemover.NullAnalyzer;

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
            if (result.NeedFix)
                _diagnosticLocations.Add(result.DiagnosticLocation!);
        }

        private void ProceedAnalyze(Func<AnalyzeResult> analyzeCall)
        {
            var analyzeResult = analyzeCall.Invoke();
            ConsumeAnalyzeResult(analyzeResult);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            base.VisitBinaryExpression(node);
            ProceedAnalyze(() => _nullAnalyzer.Analyze(node));
        }

        public override void VisitConstantPattern(ConstantPatternSyntax node)
        {
            base.VisitConstantPattern(node);
            ProceedAnalyze(() => _nullAnalyzer.Analyze(node));
        }

        public override void VisitRecursivePattern(RecursivePatternSyntax node)
        {
            base.VisitRecursivePattern(node);
            ProceedAnalyze(() => _nullAnalyzer.Analyze(node));
        }

        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            base.VisitConditionalAccessExpression(node);
            ProceedAnalyze(() => _nullAnalyzer.Analyze(node));
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            base.VisitAssignmentExpression(node);
            ProceedAnalyze(() => _nullAnalyzer.Analyze(node));
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            base.VisitSwitchStatement(node);
            ProceedAnalyze(() => _nullAnalyzer.Analyze(node));
        }
    }
}