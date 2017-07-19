using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nesp.NET4
{
    class Program
    {
        static void Main(string[] args)
        {
            var parsedString = "hello abc";
            var inputStream = new AntlrInputStream(parsedString);
            var lexer = new NespLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var parser = new NespParser(commonTokenStream);
            var graphContext = parser.id();
            Console.WriteLine(graphContext.ToStringTree(parser.RuleNames));
        }
    }
}
