using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLang.Interpreter
{
    internal abstract class Expression
    {
        public abstract Primitive Evaluate(Dictionary<string, Primitive> varEnv);
    }

    internal enum Operator { Plus, PlusPlus, Minus, MinusMinus, Times, Divide, SquareRoot, GreaterThan, LessThan, Equals, NotEquals, Not, And, Or, Modulo }


    internal class PrimitiveExpression : Expression
    {
        Primitive _value;

        public PrimitiveExpression(Primitive value)
        {
            _value = value;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv)
        {
            return _value;
        }
    }

    internal class BinaryExpression : Expression
    {
        private Expression _Left;
        private Expression _right;
        private Operator _op;

        public BinaryExpression(Expression leftExp, Operator op, Expression rightExp)
        {
            _Left = leftExp;
            _right = rightExp;
            _op = op;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv)
        {
            Primitive leftVal = _Left.Evaluate(varEnv);
            Primitive rightVal = _right.Evaluate(varEnv);

            return (leftVal, rightVal) switch
            {
                (Integer l, Integer r) => _op switch
                {
                    Operator.Plus => new Integer(l.Value + r.Value),
                    Operator.Minus => new Integer(l.Value - r.Value),
                    Operator.Times => new Integer(l.Value * r.Value),
                    Operator.Divide => new Integer(l.Value / r.Value),
                    Operator.GreaterThan => new Boolean(l.Value > r.Value),
                    Operator.LessThan => new Boolean(l.Value < r.Value),
                    Operator.Equals => new Boolean(l.Value == r.Value),
                    Operator.NotEquals => new Boolean(l.Value != r.Value),
                    Operator.Modulo => new Integer(l.Value % r.Value),
                    _ => throw new Exception("Operator not valid for 2 integers")
                },

                (Boolean l, Boolean r) => _op switch
                {
                    Operator.Equals => new Boolean(l.Value == r.Value),
                    Operator.NotEquals => new Boolean(l.Value != r.Value),
                    Operator.And => new Boolean(l.Value && r.Value),
                    Operator.Or => new Boolean(l.Value || r.Value),
                    _ => throw new Exception("Operator not valid for 2 booleans")
                },

                _ => throw new Exception("Not valid binary expression, type mismatch")
            };
        }
    }

    internal class UnaryExpression : Expression
    {
        private Expression _exp;
        private Operator _op;

        public UnaryExpression(Expression exp, Operator op)
        {
            _exp = exp;
            _op = op;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv)
        {
            Primitive val = _exp.Evaluate(varEnv);

            return val switch
            {
                Integer i => _op switch
                {
                    Operator.Minus => new Integer(-i.Value),
                    Operator.PlusPlus => new Integer(i.Value + 1),
                    Operator.MinusMinus => new Integer(i.Value - 1),
                    Operator.SquareRoot => new Integer((int)Math.Sqrt(i.Value)),
                    _ => throw new Exception("Operator not valid for 1 integer")
                },

                Boolean b => _op switch
                {
                    Operator.Not => new Boolean(!b.Value),
                    _ => throw new Exception("Operator not valid for 1 boolean")
                },

                _ => throw new Exception("Not valid binary expression, type mismatch")
            };
        }
    }

    internal class AssignmentExpression : Expression
    {
        string _varName;
        Expression _body;

        public AssignmentExpression(string varname, Expression body)
        {
            _varName = varname;
            _body = body;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv)
        {
            Primitive newVal = _body.Evaluate(varEnv);

            if (varEnv.ContainsKey(_varName))
                varEnv[_varName] = newVal;
            else
                varEnv.Add(_varName, newVal);

            return new Void();
        }
    }

    internal class VariableLookupExpression : Expression
    {
        string _varName;

        public VariableLookupExpression(string varName)
        {
            _varName = varName;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv)
        {
            if (!varEnv.ContainsKey(_varName)) throw new Exception("Variable \"" + _varName + "\" does not exist");

            return varEnv[_varName];
        }
    }

    internal class IfElseExpression : Expression
    {
        Expression _condition;
        BlockExpression _body;
        BlockExpression _else;

        public IfElseExpression(Expression condition, BlockExpression body, BlockExpression els)
        {
            _condition = condition;
            _body = body;
            _else = els;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv)
        {
            Primitive conditionVal = _condition.Evaluate(varEnv);

            return conditionVal switch
            {
                Boolean bo => bo.Value switch
                {
                    true => _body.Evaluate(varEnv),
                    false => _else.Evaluate(varEnv)
                },
                _ => throw new Exception("Condition was not a boolean")
            };
        }
    }

    internal class WhileExpression : Expression
    {
        Expression _condition;
        BlockExpression _body;

        public WhileExpression(Expression condition, BlockExpression body)
        {
            _condition = condition;
            _body = body;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv)
        {
            while (true)
            {
                Primitive conditionVal = _condition.Evaluate(varEnv);

                if (!(conditionVal is Boolean)) throw new Exception("Condition was not a boolean");

                Boolean b = (Boolean)conditionVal;
                if (b.Value)
                {
                    _body.Evaluate(varEnv);
                }
                else return new Void();
            }
        }
    }

    internal class BlockExpression : Expression
    {
        List<Expression> _body;

        public BlockExpression(List<Expression> body)
        {
            _body = body;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv)
        {
            Primitive result = new Void();

            for (int i = 0; i < _body.Count; i++)
            {
                result = _body[i].Evaluate(varEnv);
            }

            return result;
        }
    }

    internal class PrintExpression : Expression
    {
        Expression _toPrint;

        public PrintExpression(Expression toPrint)
        {
            _toPrint = toPrint;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv)
        {
            Console.WriteLine("Print: " + _toPrint.Evaluate(varEnv).ToString());

            return new Void();
        }
    }
}