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
                Fix(section) : 
                ApplyFix(caseSwitchLabelSyntax);
        }

        private Document Fix(SwitchSectionSyntax switchSectionSyntax)
        {
            var switchStatement = switchSectionSyntax.Ancestors().OfType<SwitchStatementSyntax>().First();

            return SwitchHasOnlyOneSection(switchStatement) ? 
                ApplyFix(switchStatement) : 
                ApplyFix(switchSectionSyntax);
        }

        private bool SwitchHasOnlyOneSection(SwitchStatementSyntax switchStatementSyntax)
            => switchStatementSyntax.Sections.Count == 1;
        private bool SectionHasOnlyOneLabel(SwitchSectionSyntax switchSectionSyntax) 
            => switchSectionSyntax.Labels.Count == 1;
    }
}
