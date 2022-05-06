using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PQLang.Interpreter;
using Void = PQLang.Interpreter.Void;

namespace PQLang
{
    internal static class NewParser
    {
        private static string[] blockKeywords = { "while", "fun", "else", "{" }; //Note special case for if

        private static Regex[] precedence = { 
            new Regex(@"(\\|\\|)"),         //or
            new Regex(@"(&&)"),             //and
            new Regex(@"(==|!=)"),          //equality
            new Regex(@"(<|>)"),            //greater&less
            new Regex(@"(+|-)"),            //Plus&minus
            new Regex(@"(*|/|%)"),          //Multipliying
            new Regex(@"(!(?=[^<>=]))|(((?<=[^a-zA-Z0-9_)])|(?<=\A]))[-](?=[a-zA-Z0-9_]*))|((?<=[a-zA-Z0-9_)])[-]\(.\))"), //-!
            
            new Regex(@"([(.)])+")          //Parantethis
        };

        public static Expression Parse(string program)
        {
            string denseProgram = RemoveWhiteSpace(program);

            Expression result = ParseBlock("{" + denseProgram + "}");

            Console.WriteLine(result.Unparse());
            return result;
        }

        private static Expression ParseBlock(string input)
        {
            string[] statements = SplitBlock(input);
            Expression[] expressions = new Expression[statements.Length];

            for (int i = 0; i < statements.Length; i++)
            {
                expressions[i] = ParseStatement(statements[i]);
            }

            return new BlockExpression(expressions);
        }

        private static Expression ParseStatement(string statement)
        {
            if (statement.StartsWith("{")) return ParseBlock(statement);

            string[] or = Regex.Split(statement, @"(\|\|)+").Where(p => p != "").ToArray();



            return new PrimitiveExpression(new Void());
        }

        private static string[] SplitBlock(string input)
        {
            input = input.Substring(1);
            List<string> statements = new();


            string temp = "";
            bool inString = false;
            bool inBlockStatement = false;
            bool inIf = false;
            int curlyDepth = 0;

            while (input.Length > 1)
            {
                temp += input.First();
                input = input.Substring(1);

                if (temp.Last() == '"' && !(temp[temp.Length - 2] == '\\' && inString))
                    inString = !inString;
                else if (temp.Last() == '{' && !inString)
                    curlyDepth++;
                else if (temp.Last() == '}' && !inString)
                {
                    curlyDepth--;

                    if (inBlockStatement && curlyDepth == 0)
                    {
                        if (inIf && input.StartsWith("else"))
                            inIf = false;
                        else
                            AddStatement();
                    }
                }

                if (temp.Last() == ';' && !inString && curlyDepth == 0)
                    AddStatement();
                else
                {
                    if (!inBlockStatement && temp.Length < 10)
                    {
                        if (blockKeywords.Any(s => temp.StartsWith(s)))
                            inBlockStatement = true;
                        else if (temp.StartsWith("if"))
                        {
                            inIf = true;
                            inBlockStatement = true;
                        }
                    }
                }
            }
            if (temp != "")
            {
                AddStatement();
            }

            return statements.ToArray();

            void AddStatement()
            {
                statements.Add(temp);
                temp = "";
                inBlockStatement = false;
                inIf = false;
            }
        }

        private static string RemoveWhiteSpace(string input)
        {
            string output = "";

            bool inString = false;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '"' && i > 0 && !(input[i - 1] == '\\' && inString))
                    inString = !inString;

                if (!Char.IsWhiteSpace(input[i]) || inString)
                {
                    output += input[i];
                }
            }

            return output;
        }
    }
}
