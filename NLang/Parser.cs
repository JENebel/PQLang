using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PQLang.Interpreter;

namespace PQLang
{
    internal class Pos { public int line; public int col; }

    internal static class Parser
    {
        public static Expression Parse(string program)
        {
            string trimmed = program.Replace("\t", "").Replace(Environment.NewLine, "");

            Expression result = ParseBlock(trimmed);

            return result;
        }

        private static BlockExpression ParseBlock(string block)
        {
            List<Expression> result = new List<Expression>();

            List<string> statements = new List<string>();

            int funcs = 0;
            int depth = 0;
            string temp = "";

            while (block.Length > 0)
            {
                char f = block.First();
                if (f == '{') depth++;
                else if (f == '}') depth--;

                temp += f;
                block = block.Substring(1);

                if (f == ';' && depth == 0)
                {
                    if (temp.StartsWith("fun"))
                    {
                        statements.Insert(funcs, temp.TrimEnd(';').Trim());
                        funcs++;
                    }
                    else
                        statements.Add(temp.TrimEnd(';').Trim());

                    temp = "";
                }
            }
            if (temp.StartsWith("fun"))
                statements.Insert(funcs, temp.TrimEnd(';').Trim());
            else
                statements.Add(temp.TrimEnd(';').Trim());

            foreach (string statement in statements)
            {
                if(statement != "") result.Add(ParseStatement(statement));
            }

            return new BlockExpression(result.ToArray());
        }

        private static Expression ParseStatement(string statement)
        {
            //Keywords
            if (statement.StartsWith("while")) return ParseWhile(statement);
            if (statement.StartsWith("print") && statement.Substring(5).StartsWith('(')) return ParsePrint(statement);
            if (statement.StartsWith("fun")) return ParseFuncDefinition(statement);

            //Literals
            if (statement == "true") return new PrimitiveExpression(new Interpreter.Boolean(true));
            if (statement == "false") return new PrimitiveExpression(new Interpreter.Boolean(false));
            if (statement.Length > 0 && statement.All(char.IsDigit)) return new PrimitiveExpression(new Integer(int.Parse(statement)));
            if (statement.Length > 0 && statement.All(char.IsLetter)) return new VariableLookupExpression(statement);

            //Expressions
            string rest = "";
            Expression exp = null;
            if (statement.StartsWith('!'))
            {
                var next = NextPiece(statement.Substring(1));
                exp = new UnaryExpression(ParseStatement(next.piece), Operator.Not);
                rest = next.rest;
            }
            else if (statement.StartsWith('"'))
            {
                var next = NextPiece(statement);
                exp = new PrimitiveExpression(new Interpreter.String(next.piece));
                rest = next.rest;
            }
            else if (statement.StartsWith('-'))
            {
                var next = NextPiece(statement.Substring(1));
                exp = new UnaryExpression(ParseStatement(next.piece), Operator.Minus);
                rest = next.rest;
            }
            else if (statement.StartsWith("£"))
            {
                var next = NextPiece(statement.Substring(1));
                exp = new UnaryExpression(ParseStatement(next.piece), Operator.SquareRoot);
                rest = next.rest;
            }
            else if (statement.StartsWith("if"))
            {
                statement = statement.Substring(2).Trim();

                var cond = NextPiece(statement.Trim());
                Expression condition = ParseStatement(cond.piece);

                var bod = NextBlock(cond.rest);
                BlockExpression body = ParseBlock(bod.piece);

                if (bod.rest.StartsWith("else"))
                {
                    var els = NextBlock(bod.rest.Substring(4).Trim());
                    BlockExpression elseBlock = ParseBlock(els.piece);

                    exp = new IfElseExpression(condition, body, elseBlock);
                    rest = els.rest;
                }
                else
                {
                    exp = new IfElseExpression(condition, body, new BlockExpression(new Expression[] { new PrimitiveExpression(new Interpreter.Void()) }));
                    rest = bod.rest;
                }
            }
            else
            {
                var next = NextPiece(statement.Trim());

                //Function Call
                if (next.piece.Length > 0 && next.piece.All(char.IsLetter) && next.rest.StartsWith("("))
                {
                    string fName = next.piece;
                    string argString = next.rest.TrimStart('(').TrimEnd(')');
                    string[] args = argString == "" ? new string[0] : argString.Split(',', StringSplitOptions.TrimEntries);
                    Expression[] argExps = new Expression[args.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        argExps[i] = ParseStatement(args[i]);
                    }
                    return new FunctionCallExpression(fName, argExps);
                }

                if (next.piece.Length > 0 && next.piece.All(char.IsLetter) && next.rest.StartsWith("=") && !next.rest.StartsWith("==")) return new AssignmentExpression(next.piece, ParseStatement(next.rest.Substring(1).Trim()));
                if (next.rest == "++") return new AssignmentExpression(next.piece, new UnaryExpression(new VariableLookupExpression(next.piece), Operator.PlusPlus));
                if (next.rest == "--") return new AssignmentExpression(next.piece, new UnaryExpression(new VariableLookupExpression(next.piece), Operator.MinusMinus));
                if (next.rest.StartsWith("+=")) return new AssignmentExpression(next.piece, new BinaryExpression(new VariableLookupExpression(next.piece), Operator.Plus, ParseStatement(next.rest.Substring(2).Trim())));
                if (next.rest.StartsWith("-=")) return new AssignmentExpression(next.piece, new BinaryExpression(new VariableLookupExpression(next.piece), Operator.Minus, ParseStatement(next.rest.Substring(2).Trim())));
                if (next.rest.StartsWith("*=")) return new AssignmentExpression(next.piece, new BinaryExpression(new VariableLookupExpression(next.piece), Operator.Times, ParseStatement(next.rest.Substring(2).Trim())));
                if (next.rest.StartsWith("/=")) return new AssignmentExpression(next.piece, new BinaryExpression(new VariableLookupExpression(next.piece), Operator.Divide, ParseStatement(next.rest.Substring(2).Trim())));


                exp = ParseStatement(next.piece);
                rest = next.rest;
            }

            if (rest.Length == 0)
                return exp;

            if (rest.StartsWith("+")) return new BinaryExpression(exp, Operator.Plus, ParseStatement(rest.Substring(1).Trim()));
            if (rest.StartsWith("-")) return new BinaryExpression(exp, Operator.Minus, ParseStatement(rest.Substring(1).Trim()));
            if (rest.StartsWith("*")) return new BinaryExpression(exp, Operator.Times, ParseStatement(rest.Substring(1).Trim()));
            if (rest.StartsWith("/")) return new BinaryExpression(exp, Operator.Divide, ParseStatement(rest.Substring(1).Trim()));
            if (rest.StartsWith(">")) return new BinaryExpression(exp, Operator.GreaterThan, ParseStatement(rest.Substring(1).Trim()));
            if (rest.StartsWith("<")) return new BinaryExpression(exp, Operator.LessThan, ParseStatement(rest.Substring(1).Trim()));
            if (rest.StartsWith("%")) return new BinaryExpression(exp, Operator.Modulo, ParseStatement(rest.Substring(1).Trim()));
            if (rest.StartsWith("==")) return new BinaryExpression(exp, Operator.Equals, ParseStatement(rest.Substring(2).Trim()));
            if (rest.StartsWith("!=")) return new BinaryExpression(exp, Operator.NotEquals, ParseStatement(rest.Substring(2).Trim()));
            if (rest.StartsWith("&&")) return new BinaryExpression(exp, Operator.And, ParseStatement(rest.Substring(2).Trim()));
            if (rest.StartsWith("||")) return new BinaryExpression(exp, Operator.Or, ParseStatement(rest.Substring(2).Trim()));

            throw new Exception("Could not parse");
        }

        private static Expression ParseFuncDefinition(string statement)
        {
            statement = statement.Substring(3).Trim();

            //find arg names
            var next = NextPiece(statement);
            string fName = next.piece;
            next = NextPiece(next.rest);
            string[] argNames = next.piece == "" ? new string[0] : next.piece.Split(',', StringSplitOptions.TrimEntries);
            Expression body = ParseBlock(NextBlock(next.rest).piece);

            return new FunctionDefinitionExpression(fName, argNames, body);
        }

        private static (string piece, string rest) NextPiece(string statement)
        {
            string piece = "";

            if (statement.StartsWith('!'))
            {
                piece += statement.First();
                statement = statement.Substring(1);
            }
            else if (statement.StartsWith('-'))
            {
                piece += statement.First();
                statement = statement.Substring(1);
            }

            if (statement.First() == '"')
            {
                statement = statement.Substring(1);
                char first;
                while ((first = statement.First()) != '"')
                {
                    if (statement.Length == 1) throw new Exception("Runaway string");

                    piece += statement.First();
                    statement = statement.Substring(1);
                }
                statement = statement.Substring(1);

                return (piece, statement.Trim());
            }
            else if (statement.First() == '(')
            {
                statement = statement.Substring(1);
                char first;
                int open = 1;
                while ((first = statement.First()) != ')' || open != 1)
                {
                    if (first == '(') open++;
                    else if (first == ')') open--;
                    else if (statement.Length == 1) throw new Exception("Parentheses not closed");

                    piece += statement.First();
                    statement = statement.Substring(1);
                }
                statement = statement.Substring(1);
            }
            else if (char.IsDigit(statement.First()))
            {
                while (statement.Length > 0 && char.IsDigit(statement.First()))
                {
                    piece += statement.First();
                    statement = statement.Substring(1);
                }
            }
            else if (char.IsLetter(statement.First()))
            {
                while (statement.Length > 0 && char.IsLetter(statement.First()))
                {
                    piece += statement.First();
                    statement = statement.Substring(1);
                }
            }

            return (piece.Trim(), statement.Trim());
        }

        private static (string piece, string rest) NextBlock(string statement)
        {
            string piece = "";
            if (statement.First() != '{') throw new Exception("Expected '{' but got " + statement.First());
            statement = statement.Substring(1);
            char first;
            int depth = 0;
            while ((first = statement.First()) != '}' || depth != 0)
            {
                if (statement.Length == 1) throw new Exception("Parentheses not closed");
                if (first == '{') depth++;
                else if (first == '}') depth--;

                piece += statement.First();
                statement = statement.Substring(1);
            }
            statement = statement.Substring(1);

            return (piece, statement.Trim());
        }

        private static WhileExpression ParseWhile(string statement)
        {
            statement = statement.Substring(5).Trim();

            var cond = NextPiece(statement);
            Expression condition = ParseStatement(cond.piece);

            var bod = NextBlock(cond.rest);
            BlockExpression body = ParseBlock(bod.piece);
            
            return new WhileExpression(condition, body);
        }

        private static AssignmentExpression ParseAssign(string statement)
        {
            statement = statement.Substring(3).Trim();

            var nam = NextPiece(statement);
            string name = nam.piece;

            if (!nam.rest.StartsWith('=')) throw new Exception("Not correct assignment syntax");

            Expression value = ParseStatement(nam.rest.Substring(1).Trim());

            return new AssignmentExpression(name, value);
        }

        private static PrintExpression ParsePrint(string statement)
        {
            statement = statement.Substring(5).Trim();

            Expression toPrint = ParseStatement(statement);

            return new PrintExpression(toPrint);
        }
    }
}
