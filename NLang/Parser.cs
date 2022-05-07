using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PQLang.Interpreter;
using PQLang.Errors;
using Boolean = PQLang.Interpreter.Boolean;
using String = PQLang.Interpreter.String;
using Void = PQLang.Interpreter.Void;

namespace PQLang
{
    internal static class Parser
    {
        private static readonly string[] keywords = { "while", "fun", "if", "else", "print", "sqrt", "for", "floor", "ceil", "break", "return" };
        private static readonly string[] blockKeywords = { "while", "for", "fun", "else", "{" }; //Note special case for if

        public static BlockExpression Parse(string program, string progName, List<string> includedLibs = null)
        {
            if (includedLibs == null) includedLibs = new List<string>();

            var preparedProgram = PrepareProgram(program);

            //load libs
            List<FunctionDefinitionExpression> libraryFunctions = new();

            foreach (string lib in preparedProgram.libs)
            {
                if (lib != progName && !includedLibs.Contains(lib))
                if (!File.Exists("./" + lib + ".pq")) throw new PQLangParseError(lib + ".pq could not be found");
                string libProg = File.ReadAllText("./" + lib + ".pq");
                var libs = PrepareProgram(libProg);
                includedLibs.AddRange(libs.libs);

                BlockExpression libExp = Parse(libProg, progName, includedLibs);
                libraryFunctions.AddRange(libExp.Functions);
            }

            if(libraryFunctions.Count != 0)
            {
                List<Expression> expressions = new List<Expression>(libraryFunctions);
                expressions.Add(ParseBlock("{" + preparedProgram.program + "}"));
                return new BlockExpression(expressions.ToArray());
            }
            else
                return ParseBlock("{" + preparedProgram.program + "}");
        }

        private static (string program, string[] libs) PrepareProgram(string input)
        {
            string output = "";

            bool inString = false;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '"' && i > 0 && !(input[i - 1] == '\\' && inString))
                    inString = !inString;

                if (!Char.IsWhiteSpace(input[i]) || inString || ((output.EndsWith("fun") || output.EndsWith("return")) && input[i] == ' '))
                {
                    output += input[i];
                }
            }

            List<string> libs = new();
            while (output.StartsWith("import"))
            {
                output = output.Substring(6);
                int index = output.IndexOf(";");
                libs.Add(output.Substring(0, index));
                output = output.Substring(index + 1);
            }

            return (output, libs.ToArray());
        }

        private static BlockExpression ParseBlock(string input)
        {
            string[] statements = SplitBlock(input);
            Expression[] expressions = new Expression[statements.Length];

            for (int i = 0; i < statements.Length; i++)
            {
                expressions[i] = ParseStatement(statements[i]);
            }

            return new BlockExpression(expressions);
        }

        private static bool CheckValidName(string name)
        {
            return Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$") && !keywords.Contains(name);
        }

        private static Expression ParseStatement(string statement)
        {
            if (statement == "true") return new PrimitiveExpression(new Boolean(true));
            if (statement == "false") return new PrimitiveExpression(new Boolean(false));
            if (statement == "break") return new PrimitiveExpression(new Break());
            if (statement.StartsWith("[") && statement.EndsWith("]"))
            {
                statement = statement.Substring(1, statement.Length - 2);
                return new InitArrayExpression(ParseStatement(statement)); 
            } //new array

            if (statement.StartsWith("{")) return ParseBlock(statement); //block
            if (statement.StartsWith("while("))
            {
                var next = NextParan(statement.Substring(5));
                return new WhileExpression(ParseStatement(next.paran), ParseBlock(next.rest));
            }   //while
            if (statement.StartsWith("for("))
            {
                var next = NextParan(statement.Substring(3));
                Expression[] header = SplitBlock(next.paran).Select(h => ParseStatement(h)).ToArray();
                if (header.Length == 1) return new ForLoopExpression(
                    new AssignmentExpression("0", header[0]), 
                    new BinaryExpression(new VariableLookupExpression("0"), Operator.GreaterThan, new PrimitiveExpression(new Number(0))), 
                    new AssignmentExpression("0", new UnaryExpression(new VariableLookupExpression("0"), Operator.MinusMinus)), 
                    ParseBlock(next.rest));
                
                if (header.Length == 3 && header[0] is AssignmentExpression && header[2] is AssignmentExpression)
                    return new ForLoopExpression((AssignmentExpression)header[0], header[1], (AssignmentExpression)header[2], ParseBlock(next.rest));
                else throw new PQLangParseError("Illegal for loop syntax");
            } //for
            if (statement.StartsWith("if("))
            {
                var cond = NextParan(statement.Substring(2));
                var body = NextParan(cond.rest, '{');
                if (body.rest.StartsWith("else"))
                {
                    return new IfElseExpression(ParseStatement(cond.paran.Substring(1, cond.paran.Length-2)), ParseBlock(body.paran), ParseBlock(body.rest.Substring(4)));
                }
                else
                    return new IfElseExpression(ParseStatement(cond.paran.Substring(1, cond.paran.Length - 2)), ParseBlock(body.paran),new BlockExpression(new Expression[] { new PrimitiveExpression(new Void()) }));
            } //if
            if (statement.StartsWith("print("))
            {
                return new PrintExpression(ParseStatement(statement.Substring(6, statement.Length - 7)));
            } //print
            if (statement.StartsWith("fun "))
            {
                string trimmed = statement.Substring(4);
                
                //find arg names
                int split = trimmed.IndexOf('(');
                string name = trimmed.Substring(0, split);
                var args = NextParan(trimmed.Substring(split, trimmed.Length - split));

                string[] argNames = args.paran.Substring(1, args.paran.Length - 2).Split(',');
                if (argNames.Length == 1 && argNames[0] == "") argNames = new string[0];

                foreach (var arg in argNames)
                {
                    if (!CheckValidName(arg))
                    {
                        throw new PQLangParseError("Invalid parameter name \"" + arg + "\" in fun " + name + args.paran);
                    }
                }

                if (argNames.GroupBy(x => x).Any(g => g.Count() > 1)) throw new PQLangParseError("Duplicate parameter names in: fun " + name + args.paran);

                Expression body = ParseBlock(args.rest);

                return new FunctionDefinitionExpression(name, argNames, body);
            } //FunDef
            if (statement.StartsWith("return "))
            {
                string rest = statement.Substring(7);
                if (rest.Length == 0)  return new PrimitiveExpression(new Return(new PrimitiveExpression(new Void())));
                return new PrimitiveExpression(new Return(ParseStatement(rest)));
            } //Return
            if (statement == "return")
            {
                return new PrimitiveExpression(new Return(new PrimitiveExpression(new Void())));
            } //Return
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\("))
            {
                //find arg names
                int split = statement.IndexOf('(');
                string name = statement.Substring(0, split);
                if (CheckValidName(name))
                {
                    string rest = statement.Substring(split + 1, statement.Length - split - 2);

                    Expression[] args = SplitOn(rest, ",").Where(a => a != ",").Select(a => ParseStatement(a)).ToArray();

                    return new FunctionCallExpression(name, args);
                }
            } //FunCall
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*=[^=]"))
            {
                int split = statement.IndexOf('=');
                string name = statement.Substring(0, split);
                string rest = statement.Substring(split + 1, statement.Length - split - 1);
                return new AssignmentExpression(name, ParseStatement(rest));
            } //Assign
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\+\+"))
            {
                string name = statement.Substring(0, statement.Length - 2);
                return new AssignmentExpression(name, new UnaryExpression(new VariableLookupExpression(name), Operator.PlusPlus));
            } //++
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*--"))
            {
                string name = statement.Substring(0, statement.Length - 2);
                return new AssignmentExpression(name, new UnaryExpression(new VariableLookupExpression(name), Operator.MinusMinus));
            } //--
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\+="))
            {
                string[] split = statement.Split("+=");
                if (split.Length != 2) throw new PQLangParseError("Bad syntax at: " + statement);
                return new AssignmentExpression(split[0], new BinaryExpression(new VariableLookupExpression(split[0]), Operator.Plus, ParseStatement(split[1])));
            } //+=
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*-="))
            {
                string[] split = statement.Split("-=");
                if (split.Length != 2) throw new PQLangParseError("Bad syntax at: " + statement);
                return new AssignmentExpression(split[0], new BinaryExpression(new VariableLookupExpression(split[0]), Operator.Minus, ParseStatement(split[1])));
            } //-=
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\*="))
            {
                string[] split = statement.Split("*=");
                if (split.Length != 2) throw new PQLangParseError("Bad syntax at: " + statement);
                return new AssignmentExpression(split[0], new BinaryExpression(new VariableLookupExpression(split[0]), Operator.Times, ParseStatement(split[1])));
            } //-=
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*/="))
            {
                string[] split = statement.Split("/=");
                if (split.Length != 2) throw new PQLangParseError("Bad syntax at: " + statement);
                return new AssignmentExpression(split[0], new BinaryExpression(new VariableLookupExpression(split[0]), Operator.Divide, ParseStatement(split[1])));
            } //-=

            string[] arr = SplitOn(statement, "\\|\\|");
            if(arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), "&&");
            if (arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), new string[] {"==", "!="});
            if (arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), new string[] { "<", ">", "<=", ">=" });
            if (arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), new string[] { "\\+", "-" }, @"([a-zA-Z0-9_)]$)");
            if (arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), new string[] { "\\*", "/", "%" });
            if (arr.Length > 1) return BuildTree(arr);

            arr = SplitOn(arr.First(), new string[] { "!", "-", "\\+", "sqrt", "floor", "ceil" }); //unary
            if (arr.Length > 1) return BuildTree(arr);

            if (CheckValidName(statement)) return new VariableLookupExpression(statement);
            if (statement.StartsWith("\"") && statement.EndsWith("\"")) return new PrimitiveExpression(new String(statement.Substring(1, statement.Length-2)));
            if (Regex.IsMatch(statement, @"^[0-9]+$|^[0-9]+\.[0-9]+$")) {
                return new PrimitiveExpression(new Number(float.Parse(statement)));
            }
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]+$")) return new VariableLookupExpression(statement);
            if (statement.StartsWith("(") && statement.EndsWith(")")) return ParseStatement(statement.Substring(1, statement.Length - 2));
            if (statement.EndsWith(")"))
            {
                string fName = "";
                while (!statement.StartsWith("(")) {
                    fName += statement.First();
                    statement = statement.Substring(1);
                }

                string argString = statement.Substring(1, statement.Length - 1);
                string[] args = argString == "" ? new string[0] : argString.Split(',', StringSplitOptions.TrimEntries);
                Expression[] argExps = new Expression[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    argExps[i] = ParseStatement(args[i]);
                }
                return new FunctionCallExpression(fName, argExps);
            }

            throw new PQLangParseError("Could not parse: " + statement);
        }

        private static Expression BuildTree(string[] components)
        {
            Expression Ambi(Operator op) 
            {
                if (components.Length > 2) return new BinaryExpression(Left(), op, Right());
                else return new UnaryExpression(Right(), op);
            }
            Expression Left() 
            { 
                if(components.Length > 3) 
                    return BuildTree(components.Take(components.Length - 2).ToArray());
                else
                    return ParseStatement(components.First());
            }
            Expression Right() { return ParseStatement(components.Last()); }


            string op = components[components.Length - 2];
            return op switch
            {
                "||" => new BinaryExpression(Left(), Operator.Or, Right()),
                "&&" => new BinaryExpression(Left(), Operator.And, Right()),
                "==" => new BinaryExpression(Left(), Operator.Equals, Right()),
                "!=" => new BinaryExpression(Left(), Operator.NotEquals, Right()),
                "<" => new BinaryExpression(Left(), Operator.LessThan, Right()),
                ">" => new BinaryExpression(Left(), Operator.GreaterThan, Right()),
                "<=" => new BinaryExpression(Left(), Operator.LessEqual, Right()),
                ">=" => new BinaryExpression(Left(), Operator.GreaterEqual, Right()),
                "+" => Ambi(Operator.Plus),
                "-" => Ambi(Operator.Minus),
                "*" => new BinaryExpression(Left(), Operator.Times, Right()),
                "/" => new BinaryExpression(Left(), Operator.Divide, Right()),
                "%" => new BinaryExpression(Left(), Operator.Modulo, Right()),
                "!" => new UnaryExpression(Right(), Operator.Not),
                "sqrt" => new UnaryExpression(Right(), Operator.SquareRoot),
                "floor" => new UnaryExpression(Right(), Operator.Floor),
                "ceil" => new UnaryExpression(Right(), Operator.Ceil),

                _ => throw new PQLangParseError("Could not parse \"" + op + "\" as an operator")
            };
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
                if (input.First() == '"' && !(temp.EndsWith('\\') && inString))
                    inString = !inString;
                else if (input.First() == '(' || input.First() == '[')
                    depth++;
                else if (input.First() == ')' || input.First() == ']')
                {
                    depth--;
                    if (input.Length != 1 && depth == 0)
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
                string trimMatch = match.Replace("\\", "");

                result.Add(temp);
                result.Add(trimMatch);
                input = input.Substring(trimMatch.Length);
                if(input.StartsWith("(")) depth++;
                temp = "";
            }

            if (singleParan) return SplitOn(temp.Substring(1, temp.Length - 2), separators); else return result.Where(r => r != "").ToArray();
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
            int braceDepth = 0;
            int funcs = 0;

            while (input.Length > 1)
            {
                temp += input.First();
                input = input.Substring(1);

                if (temp.Last() == '"' && !(temp[temp.Length - 2] == '\\' && inString))
                    inString = !inString;
                else if (temp.Last() == '(' && !inString)
                    braceDepth++;
                else if (temp.Last() == ')' && !inString)
                    braceDepth--;
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
                else if (temp.Last() == ';' && !inString && curlyDepth == 0 && braceDepth == 0)
                {
                    AddStatement();
                }
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

            return statements.Where(s => s != "").ToArray();

            void AddStatement()
            {
                temp = temp.TrimEnd(';');

                if (temp.StartsWith("fun"))
                {
                    statements.Insert(funcs, temp);
                    funcs++;
                }
                else
                    statements.Add(temp);
                temp = "";
                inBlockStatement = false;
                inIf = false;
            }
        }

        static (string paran, string rest) NextParan(string input, char brace = '(')
        {
            char open = brace switch
            {
                '(' => '(',
                '{' => '{',
                '[' => '[',
                _ => throw new Exception("Not a parenthesis")
            };
            char close = brace switch
            {
                '(' => ')',
                '{' => '}',
                '[' => ']',
                _ => throw new Exception("Not a parenthesis")
            };

            string paran = "";
            int depth = 0;
            bool inString = false;

            while (input.Length > 0)
            {
                paran += input.First();
                input = input.Substring(1);

                if (paran.Last() == '"' && !(paran.Length > 3 && paran[paran.Length - 2] == '\\' && inString))
                    inString = !inString;
                else if (paran.Last() == open) depth++;
                else if (paran.Last() == close)
                {
                    depth--;
                    if(depth == 0)
                        return (paran, input);
                }
            }

            throw new Exception("Unclosed parentheses at " + paran);
        }
    }
}