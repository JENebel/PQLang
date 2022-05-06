using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PQLang.Interpreter;

namespace PQLang
{
    public static class Runner
    {
        public static (string result, int time) RunFile(string fileName)
        {
            string program = File.ReadAllText(fileName);

            return Run(program);
        }

        public static (string result, int time) Run(string program)
        {
            try
            {
                Expression parsed = NewParser.Parse(program);

                Dictionary<string, Primitive> varEnv = new();
                Dictionary<string, FunctionDefinitionExpression> funEnv = new();

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Primitive result = parsed.Evaluate(varEnv, funEnv);
                stopwatch.Stop();

                return ("\nReturned: " + result.ToString(), (int)stopwatch.ElapsedMilliseconds);
            }
            catch (NLangError e)
            {
                return ("Error! " + e.Message, 0);
            }
            /*catch (Exception e)
            {
                return ("Catastrophic failure! " + e.Message, 0);
            }*/
        }
    }
}