using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullFixer
{
    public partial class SyntaxNullFixer
    {
        public Document Fix(ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax)
        {
            return _editor.OriginalDocument;
        }
    }
}
