using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLang.Interpreter;

namespace NLang
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
                Expression parsed = Parser.Parse(program);

                Dictionary<string, Primitive> varEnv = new Dictionary<string, Primitive>();

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Primitive result = parsed.Evaluate(varEnv);
                stopwatch.Stop();

                return ("Returned: " + result.ToString(), (int)stopwatch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                return ("Error! " + e.Message, 0);
            }
        }
    }
}