using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NullCheckRemover
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NullCheckRemoverAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NullCheckRemover";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private static readonly SyntaxKind[] Declarations = 
        {
            SyntaxKind.MethodDeclaration
        };
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeMethod, Declarations);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax) context.Node;
            var semanticModel = context.SemanticModel;

            var parameters = method
                .ParameterList
                .Parameters
                .Where(k =>
                {
                    var type = semanticModel.GetTypeInfo(k.Type);
                    if (type.Type.IsReferenceType)
                        return true;

                    return false;
                })
                .Select(k => semanticModel.GetDeclaredSymbol(k))
                .ToList();

            var walker = new NullCheckWalker(semanticModel, parameters);
            walker.Visit(method);
            var locations = walker.GetDiagnosticLocations();

            foreach (var location in locations)
            {
                var diagnostic = Diagnostic.Create(Rule, location, "АУФ");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
