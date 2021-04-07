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
using NullCheckRemover.NullFixer;

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
                AssignmentExpressionSyntax assignmentExpressionSyntax => fixer.Fix(assignmentExpressionSyntax)
            };

            if (nodeForFix is BinaryExpressionSyntax eq)
            {
                return fixer.Fix(eq);
            }
            else if (nodeForFix is IsPatternExpressionSyntax patter)
            {
                IsPatternFix(patter, editor);
            }
            else if (nodeForFix is SwitchExpressionArmSyntax arm)
            {
                SwitchFix(arm, editor);
            }
            else if (nodeForFix is CaseSwitchLabelSyntax label)
            {
                return fixer.Fix(label);
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

        private Document SwitchFix(SwitchExpressionArmSyntax arm, DocumentEditor editor)
        {
            editor.RemoveNode(arm);
            return editor.GetChangedDocument();
        }
    }
}
