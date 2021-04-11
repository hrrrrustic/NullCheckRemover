using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Linq;

namespace NullCheckRemover.NullFixer
{
    public partial class SyntaxNullFixer
    {
        private readonly DocumentEditor _editor;

        public SyntaxNullFixer(DocumentEditor editor)
        {
            _editor = editor;
        }

        private Document ReplaceNode(SyntaxNode oldNode, SyntaxNode newNode, bool normalizeWhitespace = true)
        {
            if (normalizeWhitespace)
                newNode = newNode.NormalizeWhitespace();

            _editor.ReplaceNode(oldNode, newNode);
            return _editor.GetChangedDocument();
        }

        private Document RemoveNode(SyntaxNode forRemoving)
        {
            _editor.RemoveNode(forRemoving);
            return _editor.GetChangedDocument();
        }

        private Document InlineNodes(SyntaxNode current, IEnumerable<SyntaxNode> statesForInline)
        {
            _editor.InsertBefore(current, statesForInline);
            _editor.RemoveNode(current);
            return _editor.GetChangedDocument();
        }
    }
}
