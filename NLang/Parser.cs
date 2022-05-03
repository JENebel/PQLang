using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLang.Interpreter;

namespace NLang
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
                    statements.Add(temp.TrimEnd(';'));
                    temp = "";
                }
            }

            statements.Add(temp);

            foreach (string statement in statements)
            {
                if(statement != "") result.Add(ParseStatement(statement));
            }

            return new BlockExpression(result);
        }

        private static Expression ParseStatement(string statement)
        {
            //Keywords
            if (statement.StartsWith("while")) return ParseWhile(statement);
            if (statement.StartsWith("var")) return ParseAssign(statement);

            //Lieterals
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
            else if (statement.StartsWith('-'))
            {
                var next = NextPiece(statement.Substring(1));
                exp = new UnaryExpression(ParseStatement(next.piece), Operator.Minus);
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
                    exp = new IfElseExpression(condition, body, new BlockExpression(new List<Expression> { new PrimitiveExpression(new Unit()) }));
                    rest = bod.rest;
                }
            }
            else
            {
                var next = NextPiece(statement.Trim());
                if (next.piece.Length > 0 && next.piece.All(char.IsLetter) && next.rest.StartsWith("=") && !next.rest.StartsWith("==")) return new AssignmentExpression(next.piece, ParseStatement(next.rest.Substring(1).Trim()));

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
            if (rest.StartsWith("==")) return new BinaryExpression(exp, Operator.Equals, ParseStatement(rest.Substring(2).Trim()));
            if (rest.StartsWith("!=")) return new BinaryExpression(exp, Operator.NotEquals, ParseStatement(rest.Substring(2).Trim()));
            if (rest.StartsWith("&&")) return new BinaryExpression(exp, Operator.And, ParseStatement(rest.Substring(2).Trim()));
            if (rest.StartsWith("||")) return new BinaryExpression(exp, Operator.Or, ParseStatement(rest.Substring(2).Trim()));

            throw new Exception("Could not parse");
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

            if (statement.First() == '(')
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
            while ((first = statement.First()) != '}')
            {
                if (statement.Length == 1) throw new Exception("Parentheses not closed");

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
    }
}
