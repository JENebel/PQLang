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
    public static class Program
    {
        public static void Main(string[] args)
        {
            string program = Path.GetFileNameWithoutExtension(args[0]);

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Expression parsed = Parser.Parse(program);
                stopwatch.Stop();
                Console.WriteLine("Succesfully parsed in " + stopwatch.ElapsedMilliseconds + "ms");
                Console.WriteLine("-------------------------------");
                Console.WriteLine();

                stopwatch.Reset();
                stopwatch.Start();
                Dictionary<string, Primitive> varEnv = new();
                Dictionary<string, FunctionDefinitionExpression> funEnv = new();
                Dictionary<string, ClassDefinitionExpression> classEnv = new();

                Primitive result = parsed.Evaluate(varEnv, funEnv, classEnv);
                if (result is Return) result = ((Return)result).Value.Evaluate(varEnv, funEnv, classEnv);
                stopwatch.Stop();

                string str = result.ToString();
                if (result is Interpreter.String) str = "\"" + str + "\"";
                Console.WriteLine();
                Console.WriteLine("-------------------------------");
                Console.WriteLine("Returned: " + str);
                Console.WriteLine("Time: " + stopwatch.ElapsedMilliseconds + "ms");
            }
            catch (PQLangParseError e)
            {
                Console.WriteLine("-------------------------------");
                string err = "Parse Error! " + e.Message;
                Console.WriteLine(err);
            }
            catch (PQLangError e)
            {
                Console.WriteLine("-------------------------------");
                string err = "Error! " + e.Message;
                Console.WriteLine(err);
            }
            catch (PQError e)
            {
                Console.WriteLine("-------------------------------");
                string err = "Program error: " + e.Message;
                Console.WriteLine(err);
            }
            
            Console.ReadLine();
        }
    }
}