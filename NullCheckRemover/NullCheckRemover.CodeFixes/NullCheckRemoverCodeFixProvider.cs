using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using NullCheckRemover.NullFixer;

namespace NullCheckRemover
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullCheckRemoverCodeFixProvider)), Shared]
    public class NullCheckRemoverCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NullCheckRemoverAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostics = context
                .Diagnostics
                .Where(k => FixableDiagnosticIds.Contains(k.Id))
                .ToList();

            var el = diagnostics.First();

            var node = root.FindNode(el.Location.SourceSpan);

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.CodeFixTitle,
                    x => RemoveRedundantNullChecks(context, node, context.CancellationToken),
                    nameof(CodeFixResources.CodeFixTitle)), 
                diagnostics);
        }

        private async Task<Document> RemoveRedundantNullChecks(CodeFixContext context, SyntaxNode nodeForFix, CancellationToken token)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, token);
            var fixer = new SyntaxNullFixer(editor);

            return nodeForFix switch
            {
                BinaryExpressionSyntax binary => fixer.Fix(binary),
                CaseSwitchLabelSyntax caseSwitch => fixer.Fix(caseSwitch),
                ConditionalAccessExpressionSyntax conditionalAccess => fixer.Fix(conditionalAccess),
                AssignmentExpressionSyntax assignmentExpressionSyntax => fixer.Fix(assignmentExpressionSyntax),
                _ => editor.OriginalDocument
            };
        }
    }
}
