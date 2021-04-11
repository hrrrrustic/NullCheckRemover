using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
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
            var diagnostic = context
                .Diagnostics
                .First(k => FixableDiagnosticIds.Contains(k.Id));

            var node = root!.FindNode(diagnostic.Location.SourceSpan);

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixResources.CodeFixTitle,
                    x => RemoveRedundantNullChecks(context, node, context.CancellationToken),
                    nameof(CodeFixResources.CodeFixTitle)), 
                diagnostic);
        }

        private async Task<Document> RemoveRedundantNullChecks(CodeFixContext context, SyntaxNode nodeForFix, CancellationToken token)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, token);
            return RemoveNullCheck(editor, nodeForFix);
        }

        private Document RemoveNullCheck(DocumentEditor editor, SyntaxNode? node)
        {
            try
            {
                var fixer = new SyntaxNullFixer(editor);
                var fix = fixer.GetFixerFor(node);
                return fix.Invoke();
            }
            // Здесь должно быть логирование как минимум. А еще неплохо было бы какое-то уведомление выдать, что не работает
            // Но не успел :(
            catch (Exception e)
            {
                return editor.OriginalDocument;
            }
        }
    }
}
