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

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeIndexer, SyntaxKind.IndexerDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeLocalMethod, SyntaxKind.LocalFunctionStatement);


            //context.RegisterSyntaxNodeAction(AnalyzeParenthesizedLambda, SyntaxKind.ParenthesizedLambdaExpression);
            //context.RegisterSyntaxNodeAction(AnalyzeSimpleLambda, SyntaxKind.SimpleLambdaExpression);
        }

        private static void AnalyzeSimpleLambda(SyntaxNodeAnalysisContext context)
        {
            var simpleLambda = (SimpleLambdaExpressionSyntax) context.Node;
            var parameters = simpleLambda.GetReferenceTypeParameters(context.SemanticModel).ToList();
            AnalyzeParameterizedMember(context, parameters);
        }

        private static void AnalyzeParenthesizedLambda(SyntaxNodeAnalysisContext context)
        {
            var parenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)context.Node;
            var parameters = parenthesizedLambda.GetReferenceTypeParameters(context.SemanticModel).ToList();
            AnalyzeParameterizedMember(context, parameters);
        }

        private static void AnalyzeLocalMethod(SyntaxNodeAnalysisContext context)
        {
            var localMethod = (LocalFunctionStatementSyntax)context.Node;
            var parameters = localMethod.GetReferenceTypeParameters(context.SemanticModel).ToList();
            AnalyzeParameterizedMember(context, parameters);
        }

        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructor = (ConstructorDeclarationSyntax) context.Node;
            var parameters = constructor.GetReferenceTypeParameters(context.SemanticModel).ToList();
            AnalyzeParameterizedMember(context, parameters);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax) context.Node;
            var parameters = method.GetReferenceTypeParameters(context.SemanticModel).ToList();
            AnalyzeParameterizedMember(context, parameters);
        }

        private static void AnalyzeIndexer(SyntaxNodeAnalysisContext context)
        {
            var indexer = (IndexerDeclarationSyntax) context.Node;
            var parameters = indexer.GetReferenceTypeParameters(context.SemanticModel).ToList();
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
                var diagnostic = Diagnostic.Create(Rule, location, "АУФ");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
