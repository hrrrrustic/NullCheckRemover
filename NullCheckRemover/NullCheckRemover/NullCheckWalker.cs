using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
            try
            {
                var analyzeResult = analyzeCall.Invoke();
                ConsumeAnalyzeResult(analyzeResult);
            }
            catch (Exception e)
            {
                // Здесь должно быть логирование
            }
        }

        public override void Visit(SyntaxNode? node)
        {
            base.Visit(node);

            var analyzer = _nullAnalyzer.GetAnalyzerFor(node);
            ProceedAnalyze(analyzer);
        }
    }
}