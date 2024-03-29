﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace NullCheckRemover
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NullCheckRemoverAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NullCheckRemover";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private readonly SyntaxKind[] _supportedNodesForAnalyze =
        {
            SyntaxKind.MethodDeclaration, SyntaxKind.ConstructorDeclaration, SyntaxKind.OperatorDeclaration, 
            SyntaxKind.ConversionOperatorDeclaration, SyntaxKind.DestructorDeclaration,SyntaxKind.LocalFunctionStatement, 
            SyntaxKind.IndexerDeclaration, SyntaxKind.ParenthesizedLambdaExpression
        };
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, _supportedNodesForAnalyze);
            context.RegisterSyntaxNodeAction(AnalyzeSimpleLambda, SyntaxKind.SimpleLambdaExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var parameters = context
                .Node
                .ChildNodes()
                .OfType<BaseParameterListSyntax>()
                .SingleOrDefault();
            
            if(parameters is null)
                return;

            var parametersForAnalyze = parameters.GetAvailableForAnalyzeParameters(context.SemanticModel).ToList();
            AnalyzeParameterizedMember(context, parametersForAnalyze);
        }

        private static void AnalyzeSimpleLambda(SyntaxNodeAnalysisContext context)
        {
            var simpleLambda = (SimpleLambdaExpressionSyntax) context.Node;
            var parameters = simpleLambda.GetAvailableForAnalyzeParameters(context.SemanticModel).ToList();
            AnalyzeParameterizedMember(context, parameters);
        }

        private static void AnalyzeParameterizedMember(SyntaxNodeAnalysisContext context, IReadOnlyList<IParameterSymbol> parameters)
        {
            if (parameters.Count == 0)
                return;

            var walker = new NullCheckWalker(context.SemanticModel, parameters);
            walker.Visit(context.Node);
            var locations = walker.GetDiagnosticLocations();

            foreach (var location in locations)
            {
                var diagnostic = Diagnostic.Create(Rule, location);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}