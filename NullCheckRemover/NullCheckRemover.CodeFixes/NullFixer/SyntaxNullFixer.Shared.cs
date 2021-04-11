using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace NullCheckRemover.NullFixer
{
    public partial class SyntaxNullFixer
    {
        private readonly DocumentEditor _editor;

        public SyntaxNullFixer(DocumentEditor editor)
        {
            _editor = editor;
        }

        private Document ApplyFix(SyntaxNode oldNode, SyntaxNode newNode)
        {
            _editor.ReplaceNode(oldNode, newNode.NormalizeWhitespace());
            return _editor.GetChangedDocument();
        }

        private Document ApplyFix(SyntaxNode forRemoving)
        {
            _editor.RemoveNode(forRemoving);
            return _editor.GetChangedDocument();
        }
    }
}
