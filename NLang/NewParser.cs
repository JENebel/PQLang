using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PQLang.Interpreter;
using Boolean = PQLang.Interpreter.Boolean;
using String = PQLang.Interpreter.String;
using Void = PQLang.Interpreter.Void;

namespace PQLang
{
    internal static class NewParser
    {
        private static string[] blockKeywords = { "while", "fun", "else", "{" }; //Note special case for if

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

            string[] arr = SplitOn(statement, "\\|\\|");
            if(arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), "&&");
            if (arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), new string[] {"==", "!="});
            if (arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), new string[] { "<", ">" });
            if (arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), new string[] { "\\+", "-" }, @"([a-zA-Z0-9_)]$)");
            if (arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), new string[] { "\\*", "/", "%" });
            if (arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), new string[] { "!", "-", "\\+" }); //unary minus & plus
            if (arr.Length > 1) return BuildTree(arr);

            string atom = arr[0];

            if (atom == "true") return new PrimitiveExpression(new Boolean(true));
            if (atom == "false") return new PrimitiveExpression(new Boolean(false));
            if (atom.StartsWith("\"") && atom.EndsWith("\"")) return new PrimitiveExpression(new String(atom));
            if (Regex.IsMatch(atom, @"^[0-9]+$")) return new PrimitiveExpression(new Integer(int.Parse(atom)));
            if (Regex.IsMatch(atom, @"^[a-zA-Z_][a-zA-Z0-9_]+$")) return new VariableLookupExpression(atom);
            if (atom.EndsWith(")"))
            {
                string fName = "";
                while (!atom.StartsWith("(")) {
                    fName += atom.First();
                    atom = atom.Substring(1);
                }

                string argString = atom.Substring(1, atom.Length - 1);
                string[] args = argString == "" ? new string[0] : argString.Split(',', StringSplitOptions.TrimEntries);
                Expression[] argExps = new Expression[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    argExps[i] = ParseStatement(args[i]);
                }
                return new FunctionCallExpression(fName, argExps);
            }

            return new PrimitiveExpression(new Void());
        }

        private static Expression BuildTree(string[] componets)
        {
            return new PrimitiveExpression(new Void());
        }

        private static string[] SplitOn(string input, string separator) { return SplitOn(input, new string[] { separator }); }

        private static string[] SplitOn(string input, string[] separators, string cond = "")
        {
            List<string> result = new();

            string temp = "";
            bool inString = false;
            bool singleParan = input.StartsWith("(");
            int depth = 0;

            while (input.Length > 0)
            {
                if (input.First() == '"' && !(temp.Length > 0 && temp.Last() == '\\' && inString))
                    inString = !inString;
                else if (input.First() == '(')
                    depth++;
                else if (input.First() == ')')
                {
                    depth--;
                    if (input.Length != 1)
                        singleParan = false;
                }

                if (depth == 0 && !inString) CheckAdd();

                if (input.Length > 0)
                {
                    temp += input.First();
                    input = input.Substring(1);
                }
            }
            result.Add(temp);

            void CheckAdd()
            {
                string match = separators.Where(s => Regex.IsMatch(input, "^" + s) && (cond == "" || Regex.IsMatch(temp, cond))).FirstOrDefault("");
                if (match == "") return;

                result.Add(temp);
                result.Add(match);
                input = input.Substring(match.Length);
                temp = "";
            }

            if (singleParan) return SplitOn(temp, separators); else return result.Where(r => r != "").ToArray();
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
