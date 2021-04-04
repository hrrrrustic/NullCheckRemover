using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = NullCheckRemover.Test.CSharpCodeFixVerifier<
    NullCheckRemover.NullCheckRemoverAnalyzer,
    NullCheckRemover.NullCheckRemoverCodeFixProvider>;

namespace NullCheckRemover.Test
{
    [TestClass]
    public class NullCheckRemoverUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
    using System;

namespace Test
{
    class Program
    {
        static void Main2(string[]? args)
        {
            if (args == null) { }
            if (args != null) { }
            if (null == args) { }
            if (null != args) { }
            if (args == default) { }
            if (args != default) { }
            if (default == args) { }
            if (default != args) { }
            if (args is {}) { }
            if (args is null) { }
            if (args is not null) { }
            var t = args?.GetLength(0);
            var a = args ?? Array.Empty<string>();
            args ??= Array.Empty<string>();
            switch (args)
            {
                case null:
                    break;
            }
            var q = args switch
            {
                null => true,
                _ => throw new NotImplementedException()
            };
        }
    }
}";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("NullCheckRemover").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task Test()
        {
                var source = @"
    using System;

namespace Test
{
    class Program
    {
        static void Main2(string[]? args)
        {
            if (args == null) { }
            if (args != null) { }
            if (null == args) { }
            if (null != args) { }
            if (args == default) { }
            if (args != default) { }
            if (default == args) { }
            if (default != args) { }
            if (args is {}) { }
            if (args is null) { }
            if (args is not null) { }
            var t = args?.GetLength(0);
            var a = args ?? Array.Empty<string>();
            args ??= Array.Empty<string>();
            switch (args)
            {
                case null:
                    break;
            }
            var q = args switch
            {
                null => true,
                _ => throw new NotImplementedException()
            };
        }
    }
}";
                await VerifyCS.VerifyAnalyzerAsync(source);

        }
    }
}
