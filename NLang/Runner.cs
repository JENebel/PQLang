using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLang.Interpreter;

namespace NLang
{
    public static class Runner
    {
        public static string RunFile(string fileName)
        {
            string program = File.ReadAllText(fileName);

            return Run(program);
        }

        public static string Run(string program)
        {
            try
            {
                Expression parsed = Parser.Parse(program);

                Dictionary<string, Primitive> varEnv = new Dictionary<string, Primitive>();

                Primitive result = parsed.Evaluate(varEnv);

                return "Returned: " + result switch
                {
                    Integer i => "Int: " + i.Value,
                    Interpreter.Boolean b => "Bool: " + b.Value,
                    Unit => "Unit"
                };
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}