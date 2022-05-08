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
        private static readonly string[] keywords = { "true", "false", "while", "fun", "if", "else", "print", "sqrt", "for", "floor", "ceil", "break", "return", "class", "new", "type", "error", "read" };
        private static readonly string[] blockKeywords = { "while", "for", "fun", "else", "{", "class" }; //Note special case for if

        public static BlockExpression Parse(string programName, List<string> includedLibs = null)
        {
            if (!File.Exists("./" + programName + ".pq")) throw new PQLangParseError(programName + ".pq could not be found");
            string[] progLines = File.ReadLines("./" + programName + ".pq").ToArray();
            progLines = progLines.Select(line => line.Split("//")[0]).ToArray();

            string program = string.Join("", progLines);

            if (includedLibs == null) includedLibs = new List<string>();

            var preparedProgram = PrepareProgram(program);

            //load libs
            List<ClassDefinitionExpression> libraryClasses = new();
            List<FunctionDefinitionExpression> libraryFunctions = new();

            foreach (string lib in preparedProgram.libs)
            {
                if (lib != program && !includedLibs.Contains(lib))
                {
                    var libs = PrepareProgram(lib).libs;
                    includedLibs.AddRange(libs);

                    BlockExpression libExp = Parse(lib, includedLibs);
                    libraryFunctions.AddRange(libExp.Functions);
                    libraryClasses.AddRange(libExp.Classes);
                }
            }

            if(libraryFunctions.Count != 0 || libraryClasses.Count != 0)
            {
                List<Expression> expressions = new List<Expression>();
                expressions.AddRange(libraryClasses);
                expressions.AddRange(libraryFunctions);
                expressions.Add(ParseBlock("{" + preparedProgram.program + "}"));
                return new BlockExpression(expressions);
            }
            else
                return ParseBlock("{" + preparedProgram.program + "}");
        }

        private static (string program, string[] libs) PrepareProgram(string input)
        {
            string output = "";
            bool inComment = false;
            bool inString = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '"' && i > 0 && !(input[i - 1] == '\\' && inString))
                    inString = !inString;
                else if (i + 1 < input.Length && input[i] == '/' && input[i + 1] == '*')
                    inComment = true;
                else if (i + 1 < input.Length && input[i] == '*' && input[i + 1] == '/')
                {
                    i += 2;
                }

                if (!inComment && (!Char.IsWhiteSpace(input[i]) || inString || ((output.EndsWith("fun") || output.EndsWith("return") || output.EndsWith("class") || output.EndsWith("new")) && input[i] == ' ')))
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

            return new BlockExpression(expressions.ToList());
        }

        private static bool CheckValidName(string name)
        {
            return Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$") && !keywords.Contains(name);
        }

        private static Expression ParseStatement(string statement)
        {
            if (statement == "true") return new PrimitiveExpression(new Boolean(true));
            if (statement == "false") return new PrimitiveExpression(new Boolean(false));
            if (statement == "read") return new ReadExpression();
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
                    return new IfElseExpression(ParseStatement(cond.paran.Substring(1, cond.paran.Length - 2)), ParseBlock(body.paran), ParseBlock(body.rest.Substring(4)));
                }
                else
                    return new IfElseExpression(ParseStatement(cond.paran.Substring(1, cond.paran.Length - 2)), ParseBlock(body.paran),new BlockExpression(new List<Expression> { new PrimitiveExpression(new Void()) }));
            } //if
            if (statement.StartsWith("print("))
            {
                return new PrintExpression(ParseStatement(statement.Substring(6, statement.Length - 7)));
            } //print
            if (statement.StartsWith("error("))
            {
                return new ErrorExpression(ParseStatement(statement.Substring(6, statement.Length - 7)));
            } //error
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
            } //ClassDef
            if (statement.StartsWith("class "))
            {
                string trimmed = statement.Substring(6);

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
                        throw new PQLangParseError("Invalid parameter name \"" + arg + "\" in class " + name + args.paran);
                    }
                }

                if (argNames.GroupBy(x => x).Any(g => g.Count() > 1)) throw new PQLangParseError("Duplicate parameter names in: class " + name + args.paran);

                Expression body = ParseBlock(args.rest);
                if (body is not BlockExpression) body = new BlockExpression(new List<Expression>() { body });

                return new ClassDefinitionExpression(name, argNames, (BlockExpression)body);
            } //FunDef
            if (statement.StartsWith("new "))
            {
                //find arg names
                int split = statement.IndexOf('(');
                string name = statement.Substring(4, split - 4);
                if (CheckValidName(name))
                {
                    string rest = statement.Substring(split + 1, statement.Length - split - 2);

                    Expression[] args = SplitOn(rest, ",").Where(a => a != ",").Select(a => ParseStatement(a)).ToArray();

                    return new ClassInstantiateExpression(name, args);
                }
                else throw new PQLangParseError("Invalid class name \"" + name +"\"");
            } //New object
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
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*=[^=]"))
            {
                int split = statement.IndexOf('=');
                string name = statement.Substring(0, split);
                string rest = statement.Substring(split + 1, statement.Length - split - 1);
                return new AssignmentExpression(name, ParseStatement(rest));
            } //Assign
            
            #region Operator+Assign
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\+\+$"))
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
                int split = statement.IndexOf("+=");
                string name = statement.Substring(0, split);
                string rest = statement.Substring(split + 2, statement.Length-split-2);
                return new AssignmentExpression(name, new BinaryExpression(new VariableLookupExpression(name), Operator.Plus, ParseStatement(rest)));
            } //+=
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*-="))
            {
                int split = statement.IndexOf("-=");
                string name = statement.Substring(0, split);
                string rest = statement.Substring(split + 2, statement.Length - split - 2);
                return new AssignmentExpression(name, new BinaryExpression(new VariableLookupExpression(name), Operator.Minus, ParseStatement(rest)));
            } //-=
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\*="))
            {
                int split = statement.IndexOf("*=");
                string name = statement.Substring(0, split);
                string rest = statement.Substring(split + 2, statement.Length - split - 2);
                return new AssignmentExpression(name, new BinaryExpression(new VariableLookupExpression(name), Operator.Times, ParseStatement(rest)));
            } //-=
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*/="))
            {
                int split = statement.IndexOf("/=");
                string name = statement.Substring(0, split);
                string rest = statement.Substring(split + 2, statement.Length - split - 2);
                return new AssignmentExpression(name, new BinaryExpression(new VariableLookupExpression(name), Operator.Divide, ParseStatement(rest)));
            } //-=
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\[.\]\+\+$"))
            {
                int split = statement.IndexOf('[');
                string name = statement.Substring(0, split);
                var index = NextParan(statement.Substring(split, statement.Length - split), '[');
                Expression indExp = ParseStatement(index.paran.Substring(1, index.paran.Length - 2));
                return new ArraySetExpression(name, indExp, new UnaryExpression(new ArrayLookUpExpression(name, indExp), Operator.PlusPlus));
            } //a[]++
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\[.\]--$"))
            {
                int split = statement.IndexOf('[');
                string name = statement.Substring(0, split);
                var index = NextParan(statement.Substring(split, statement.Length - split), '[');
                Expression indExp = ParseStatement(index.paran.Substring(1, index.paran.Length - 2));
                return new ArraySetExpression(name, indExp, new UnaryExpression(new ArrayLookUpExpression(name, indExp), Operator.MinusMinus));
            } //a[]--
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\[.\]\+="))
            {
                int split = statement.IndexOf("+=");
                string square = statement.Substring(0, split);
                string rest = statement.Substring(split + 2, statement.Length - split - 2);
                split = square.IndexOf('[');
                string name = square.Substring(0, split);
                Expression index = ParseStatement(square.Substring(split + 1, square.Length - split - 2));
                return new ArraySetExpression(name, index, new BinaryExpression(new ArrayLookUpExpression(name, index), Operator.Plus, ParseStatement(rest)));
            } //a[]+=
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\[.\]-="))
            {
                int split = statement.IndexOf("-=");
                string square = statement.Substring(0, split);
                string rest = statement.Substring(split + 2, statement.Length - split - 2);
                split = square.IndexOf('[');
                string name = square.Substring(0, split);
                Expression index = ParseStatement(square.Substring(split + 1, square.Length - split - 2));
                return new ArraySetExpression(name, index, new BinaryExpression(new ArrayLookUpExpression(name, index), Operator.Minus, ParseStatement(rest)));
            } //a[]-=
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\[.\]\*="))
            {
                int split = statement.IndexOf("*=");
                string square = statement.Substring(0, split);
                string rest = statement.Substring(split + 2, statement.Length - split - 2);
                split = square.IndexOf('[');
                string name = square.Substring(0, split);
                Expression index = ParseStatement(square.Substring(split + 1, square.Length - split - 2));
                return new ArraySetExpression(name, index, new BinaryExpression(new ArrayLookUpExpression(name, index), Operator.Times, ParseStatement(rest)));
            } //a[]*=
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\[.\]/="))
            {
                int split = statement.IndexOf("/=");
                string square = statement.Substring(0, split);
                string rest = statement.Substring(split + 2, statement.Length - split - 2);
                split = square.IndexOf('[');
                string name = square.Substring(0, split);
                Expression index = ParseStatement(square.Substring(split + 1, square.Length - split - 2));
                return new ArraySetExpression(name, index, new BinaryExpression(new ArrayLookUpExpression(name, index), Operator.Divide, ParseStatement(rest)));
            } //a[]/=
            #endregion

            #region Operators
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
            #endregion

            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\("))
            {
                //find arg names
                int split = statement.IndexOf('(');
                string name = statement.Substring(0, split);
                string rest = statement.Substring(split + 1, statement.Length - split - 2);

                Expression[] args = SplitOn(rest, ",").Where(a => a != ",").Select(a => ParseStatement(a)).ToArray();

                return new FunctionCallExpression(name, args);
            } //FunCall
            if (Regex.IsMatch(statement, @"\.[a-zA-Z_][a-zA-Z0-9_]*=[^=].*$"))
            {
                int split = statement.IndexOf('=');
                string pre = statement.Substring(0, split);
                int splitPre = pre.LastIndexOf('.');
                string before = pre.Substring(0, splitPre);
                string name = pre.Substring(splitPre + 1, pre.Length - splitPre - 1);
                string rest = statement.Substring(split + 1, statement.Length - split - 1);
                return new ClassSetFieldExpression(ParseStatement(before), name, ParseStatement(rest));
            } //AssignField
            if (Regex.IsMatch(statement, @"\.[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                int split = statement.LastIndexOf('.');
                string before = statement.Substring(0, split);
                string name = statement.Substring(split + 1, statement.Length - split - 1);
                string rest = statement.Substring(split + 1, statement.Length - split - 1);
                return new ClassGetFieldExpression(ParseStatement(before), name);
            } //GetField
            if (Regex.IsMatch(statement, @"\.[a-zA-Z_][a-zA-Z0-9_]*\(.*\)$"))
            {
                int split = statement.LastIndexOf('.');
                string obj = statement.Substring(0, split);
                string func = statement.Substring(split + 1, statement.Length - split - 1);

                split = func.IndexOf('(');
                string name = func.Substring(0, split);
                string rest = func.Substring(split + 1, func.Length - split - 2);

                Expression[] args = SplitOn(rest, ",").Where(a => a != ",").Select(a => ParseStatement(a)).ToArray();

                return new ClassCallMethodExpression(ParseStatement(obj), name, args);
            } //ClassAccesMethod
            if (CheckValidName(statement)) return new VariableLookupExpression(statement);
            if (statement.StartsWith("\"") && statement.EndsWith("\"")) return new PrimitiveExpression(new String(statement.Substring(1, statement.Length-2)));
            if (Regex.IsMatch(statement, @"^[0-9]+$|^[0-9]+\.[0-9]+$")) {
                return new PrimitiveExpression(new Number(float.Parse(statement)));
            }
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\[.+\]="))
            {
                int split = statement.IndexOf('[');
                string name = statement.Substring(0, split);
                var index = NextParan(statement.Substring(split, statement.Length - split), '[');
                return new ArraySetExpression(name, ParseStatement(index.paran.Substring(1, index.paran.Length - 2)), ParseStatement(index.rest.Substring(1)));
            } //Array set
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]*\[.+\]$"))
            {
                int split = statement.IndexOf('[');
                string name = statement.Substring(0, split);
                string rest = statement.Substring(split + 1, statement.Length - split - 2);
                return new ArrayLookUpExpression(name, ParseStatement(rest));
            } //Array lookup
            if (Regex.IsMatch(statement, @"^[a-zA-Z_][a-zA-Z0-9_]+$")) return new VariableLookupExpression(statement); //VarLookUp
            if (statement.StartsWith("(") && statement.EndsWith(")")) return ParseStatement(statement.Substring(1, statement.Length - 2)); //Paran

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

                if (match.Length == 1 && input.Length > 1 &&  separators.Contains(input.Substring(0, 2)))
                {
                    match = input.Substring(0, 2);
                    input = input.Substring(2);
                }
                else
                {
                    match = match.Replace("\\", "");
                    input = input.Substring(match.Length);
                }

                result.Add(temp);
                result.Add(match);
                
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
            int classes = 0;

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

                if (temp.StartsWith("class"))
                {
                    statements.Insert(classes, temp);
                    classes++;
                }
                else if (temp.StartsWith("fun"))
                {
                    statements.Insert(funcs + classes, temp);
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