using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;

namespace NullCheckRemover
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullCheckRemoverCodeFixProvider)), Shared]
    public class NullCheckRemoverCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(NullCheckRemoverAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Debugger.Launch();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostics = context
                .Diagnostics
                .Where(k => FixableDiagnosticIds.Contains(k.Id))
                .ToList();

            var locations = diagnostics.Select(k => k.Location).ToList();
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.CodeFixTitle,
                    x => RemoveRedundantNullChecks(context, locations, context.CancellationToken),
                    nameof(CodeFixResources.CodeFixTitle)), 
                diagnostics);
        }

        private async Task<Solution> MakeUppercaseAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var identifierToken = typeDecl.Identifier;
            var newName = identifierToken.Text.ToUpperInvariant();

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }

        private async Task<Document> RemoveRedundantNullChecks(CodeFixContext context, IReadOnlyList<Location> locations, CancellationToken token)
        {
            var root = await context.Document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            var editor = await DocumentEditor.CreateAsync(context.Document, token);
            foreach (Location location in locations)
            {
                var node = root.FindNode(location.SourceSpan);
                if (node is BinaryExpressionSyntax eq)
                {
                    BinaryFix(eq, editor);
                }
                else if (node is IsPatternExpressionSyntax patter)
                {
                    IsPatternFix(patter, editor);
                }
                else if (node is SwitchExpressionArmSyntax arm)
                {
                    SwitchFix(arm, editor);
                }
                else if (node is CaseSwitchLabelSyntax label)
                {
                    SwitchFix(label, editor);
                }
            }

            return editor.GetChangedDocument();
        }

        private void IsPatternFix(IsPatternExpressionSyntax pattern, DocumentEditor editor)
        {
            if (pattern.Pattern is ConstantPatternSyntax constant)
            {
                var trueExpression = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                editor.ReplaceNode(pattern, trueExpression);
            }
            else if (pattern.Pattern is UnaryPatternSyntax unary)
            {
                var falseExpression = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                editor.ReplaceNode(pattern, falseExpression);
            }
            else if(pattern.Pattern is RecursivePatternSyntax recursive)
            {
                var trueExpression = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                editor.ReplaceNode(pattern, trueExpression);
            }
        }

        private void BinaryFix(BinaryExpressionSyntax eq, DocumentEditor editor)
        {
            if (eq.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken))
            {
                var trueExpression = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                editor.ReplaceNode(eq, trueExpression);
            }
            else if(eq.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken))
            {
                var falseExpression = SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                editor.ReplaceNode(eq, falseExpression);
            }
            else
            {
                var withoutCoalesce = eq.Right;
                editor.ReplaceNode(eq, withoutCoalesce);
            }
        }

        private Document SwitchFix(SwitchExpressionArmSyntax arm, DocumentEditor editor)
        {
            editor.RemoveNode(arm);
            return editor.GetChangedDocument();
        }

        private Document SwitchFix(CaseSwitchLabelSyntax label, DocumentEditor editor)
        {
            var section = label.Ancestors().OfType<SwitchSectionSyntax>().First();
            if (section.Labels.Count == 1)
            {
                editor.RemoveNode(section);
            }
            else
            {
                editor.RemoveNode(label);
            }

            return editor.GetChangedDocument();
        }
    }
}
