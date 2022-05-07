using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PQLang.Errors;
using PQLang.Interpreter;

namespace PQLang
{
    public static class Runner
    {
        public static (string result, int time) RunFile(string fileName)
        {
            string program = File.ReadAllText(fileName);

            return Run(program, Path.GetFileNameWithoutExtension(fileName));
        }

        public static (string result, int time) Run(string program, string progName = "")
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Expression parsed = Parser.Parse(program, progName);
                stopwatch.Stop();
                Console.WriteLine("Succesfully parsed in " + stopwatch.ElapsedMilliseconds + "ms");
                Console.WriteLine("-------------------------------");
                Console.WriteLine();

                stopwatch.Reset();
                stopwatch.Start();
                Dictionary<string, Primitive> varEnv = new();
                Dictionary<string, FunctionDefinitionExpression> funEnv = new();

                Primitive result = parsed.Evaluate(varEnv, funEnv);
                if (result is Return) result = ((Return)result).Value.Evaluate(varEnv, funEnv);
                stopwatch.Stop();

                string str = result.ToString();
                if (result is Interpreter.String) str = "\"" + str + "\"";
                Console.WriteLine();
                Console.WriteLine("-------------------------------");
                Console.WriteLine("Returned: " + str);
                Console.WriteLine("Time: " + stopwatch.ElapsedMilliseconds + "ms");
                return ("Returned: " + str, (int)stopwatch.ElapsedMilliseconds);
            }
            catch (PQLangParseError e)
            {
                return ("Parse Error! " + e.Message, 0);
            }
            catch (PQLangError e)
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