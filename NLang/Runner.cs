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
                Expression parsed = NewParser.Parse(program, progName);
                Console.WriteLine("Succesfully parsed");

                Dictionary<string, Primitive> varEnv = new();
                Dictionary<string, FunctionDefinitionExpression> funEnv = new();

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Primitive result = parsed.Evaluate(varEnv, funEnv);
                stopwatch.Stop();

                string str = result.ToString();
                if (result is Interpreter.String) str = "\"" + str + "\"";
                return ("\nReturned: " + str, (int)stopwatch.ElapsedMilliseconds);
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