using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NullCheckRemover.NullFixer
{
    public partial class SyntaxNullFixer
    {
        public Document Fix(CaseSwitchLabelSyntax caseSwitchLabelSyntax)
        {
            var section = caseSwitchLabelSyntax.Ancestors().OfType<SwitchSectionSyntax>().First();
            return SectionHasOnlyOneLabel(section) ? 
                ApplyFix(section) : 
                ApplyFix(caseSwitchLabelSyntax);
        }

        private bool SectionHasOnlyOneLabel(SwitchSectionSyntax switchSectionSyntax) 
            => switchSectionSyntax.Labels.Count == 1;
    }
}
