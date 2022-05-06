using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQLang.Interpreter
{
    internal abstract class Expression
    {
        public abstract Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv);

        public abstract string Unparse();
    }

    internal enum Operator { Plus, PlusPlus, Minus, MinusMinus, Times, Divide, SquareRoot, GreaterThan, LessThan, Equals, NotEquals, Not, And, Or, Modulo }

    internal class PrimitiveExpression : Expression
    {
        Primitive _value;

        public PrimitiveExpression(Primitive value)
        {
            _value = value;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv)
        {
            return _value;
        }

        public override string Unparse()
        {
            return _value.ToString();
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv)
        {
            Primitive leftVal = _Left.Evaluate(varEnv, funEnv);
            Primitive rightVal = _right.Evaluate(varEnv, funEnv);

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
                    _ => throw new NLangError("Operator " + _op.ToString() + " not valid for 2 Integers")
                },

                (Boolean l, Boolean r) => _op switch
                {
                    Operator.Equals => new Boolean(l.Value == r.Value),
                    Operator.NotEquals => new Boolean(l.Value != r.Value),
                    Operator.And => new Boolean(l.Value && r.Value),
                    Operator.Or => new Boolean(l.Value || r.Value),
                    _ => throw new NLangError("Operator " + _op.ToString() + " not valid for 2 Booleans")
                },

                //String concat
                (String l, Primitive r) => _op switch
                {
                    Operator.Plus => new String(l.Value + r.ToString()),
                    _ => throw new NLangError("Operator " + _op.ToString() + " not valid for String and " + r.Type())
                },
                (Primitive l, String r) => _op switch
                {
                    Operator.Plus => new String(l.ToString() + r.Value),
                    _ => throw new NLangError("Operator " + _op.ToString() + " not valid for " + l.Type() +" and String")
                },

                _ => _op switch
                {
                    Operator.Equals => new Boolean(false),
                    _ => throw new NLangError("Operator " + _op.ToString() + " not valid for " + leftVal.Type() + " and " + rightVal.Type())
                }
            };
        }

        public override string Unparse()
        {
            return "(" + _Left.Unparse() + " " + _op.ToString() + ")";
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv)
        {
            Primitive val = _exp.Evaluate(varEnv, funEnv);

            return val switch
            {
                Integer i => _op switch
                {
                    Operator.Minus => new Integer(-i.Value),
                    Operator.PlusPlus => new Integer(i.Value + 1),
                    Operator.MinusMinus => new Integer(i.Value - 1),
                    Operator.SquareRoot => new Integer((int)Math.Sqrt(i.Value)),
                    _ => throw new NLangError("Operator not valid for 1 integer")
                },

                Boolean b => _op switch
                {
                    Operator.Not => new Boolean(!b.Value),
                    _ => throw new NLangError("Operator not valid for 1 boolean")
                },

                _ => throw new NLangError("Not valid binary expression, type mismatch")
            };
        }

        public override string Unparse()
        {
            return _op.ToString() + " " + _exp.Unparse();
        }
    }

    internal class FunctionDefinitionExpression : Expression
    {
        string _funName;
        public string[] Arguments { get; }
        public Expression Body { get; }

        public FunctionDefinitionExpression(string funName, string[] arguments, Expression body)
        {
            _funName = funName;
            Arguments = arguments;
            Body = body;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv)
        {
            if (varEnv.ContainsKey(_funName)) throw new NLangError("Function \"" + _funName +"\" already exists");

            funEnv.Add(_funName, this);

            return new Void();
        }

        public override string Unparse()
        {
            return "fun " + _funName + "(" + string.Join(',', Arguments) + ");";
        }
    }

    internal class FunctionCallExpression : Expression
    {
        string _funName;
        Expression[] _arguments;

        public FunctionCallExpression(string funName, Expression[] arguments)
        {
            _funName = funName;
            _arguments = arguments;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv)
        {
            if (!funEnv.ContainsKey(_funName)) throw new NLangError("No such function \"" + _funName + "\"");
            FunctionDefinitionExpression func = funEnv[_funName];
            if (func.Arguments.Length != _arguments.Length) throw new NLangError("Expected " + func.Arguments.Length + " arguments for function \"" + _funName +  "\" but got " + _arguments.Length);

            Dictionary<string, Primitive> newVarEnv = new Dictionary<string, Primitive>(varEnv);

            for (int i = 0; i < _arguments.Count(); i++)
            {
                newVarEnv[func.Arguments[i]] = _arguments[i].Evaluate(varEnv, funEnv);
            }

            var result = func.Body.Evaluate(newVarEnv, funEnv);
            return result;
        }

        public override string Unparse()
        {
            return _funName + "(" + string.Join(',', _arguments.Select(exp => exp.Unparse())) + ")";
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv)
        {
            Primitive newVal = _body.Evaluate(varEnv, funEnv);

            if (varEnv.ContainsKey(_varName))
                varEnv[_varName] = newVal;
            else
                varEnv.Add(_varName, newVal);

            return new Void();
        }

        public override string Unparse()
        {
            return _varName + "=" + _body.Unparse() + ";";
        }
    }

    internal class VariableLookupExpression : Expression
    {
        string _varName;

        public VariableLookupExpression(string varName)
        {
            _varName = varName;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv)
        {
            if (!varEnv.ContainsKey(_varName)) throw new NLangError("Variable \"" + _varName + "\" does not exist");

            return varEnv[_varName];
        }

        public override string Unparse()
        {
            return _varName;
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv)
        {
            Primitive conditionVal = _condition.Evaluate(varEnv, funEnv);

            return conditionVal switch
            {
                Boolean bo => bo.Value switch
                {
                    true => _body.Evaluate(varEnv, funEnv),
                    false => _else.Evaluate(varEnv, funEnv)
                },
                _ => throw new NLangError("Condition was not a boolean")
            };
        }

        public override string Unparse()
        {
            return "if(" + _condition.Unparse() + "){" + _body.Unparse() + "}else{" + _else.Unparse() + "};";
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv)
        {
            while (true)
            {
                Primitive conditionVal = _condition.Evaluate(varEnv, funEnv);

                if (!(conditionVal is Boolean)) throw new NLangError("Condition was not a boolean");

                Boolean b = (Boolean)conditionVal;
                if (b.Value)
                {
                    _body.Evaluate(varEnv, funEnv);
                }
                else return new Void();
            }
        }

        public override string Unparse()
        {
            return "while(" + _condition.Unparse() + "){" + _body.Unparse() + "};";
        }
    }

    internal class BlockExpression : Expression
    {
        Expression[] _body;

        public BlockExpression(Expression[] body)
        {
            _body = body;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv)
        {
            Primitive result = new Void();

            foreach (Expression expression in _body)
            {
                result = expression.Evaluate(varEnv, funEnv);
            }

            return result;
        }

        public override string Unparse()
        {
            return "{" + string.Join(";\n", _body.Select(exp => exp.Unparse())) + "}";
        }
    }

    internal class PrintExpression : Expression
    {
        Expression _toPrint;

        public PrintExpression(Expression toPrint)
        {
            _toPrint = toPrint;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv)
        {
            Console.WriteLine(_toPrint.Evaluate(varEnv, funEnv).ToString());

            return new Void();
        }

        public override string Unparse()
        {
            return _toPrint.Unparse();
        }
    }
}