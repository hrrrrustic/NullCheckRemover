using System;
using Microsoft.CodeAnalysis;

namespace NullCheckRemover
{
    public readonly struct AnalyzeResult
    {
        public Location? DiagnosticLocation { get; }
        public bool NeedFix { get; }

        private AnalyzeResult(Location? diagnosticLocation, Boolean needFix)
        {
            DiagnosticLocation = diagnosticLocation;
            NeedFix = needFix;
        }

        public static AnalyzeResult False() => new(null, false);
        public static AnalyzeResult True(Location location) => new(location, true);

        public void DoIfPositive(Action<AnalyzeResult> action)
        {
            if(NeedFix)
                action.Invoke(this);
        }
    }
}