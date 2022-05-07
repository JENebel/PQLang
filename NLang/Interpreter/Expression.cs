using PQLang.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQLang.Interpreter
{
    internal abstract class Expression
    {
        public abstract Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv);

        public abstract string Unparse();

        internal Primitive VarEnvLookup(string varName, (Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv)
        {
            if (varEnv.outer.ContainsKey(varName)) return varEnv.outer[varName];
            if (varEnv.local.ContainsKey(varName)) return varEnv.local[varName];

            throw new PQLangError("Variable \"" + varName + "\" does not exist in the current scope");
        }

        internal FunctionDefinitionExpression FunEnvLookup(string varName, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            if (funEnv.outer.ContainsKey(varName)) return funEnv.outer[varName];
            if (funEnv.local.ContainsKey(varName)) return funEnv.local[varName];

            throw new PQLangError("Function \"" + varName + "\" does not exist in the current scope");
        }

        internal (Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) CombineVarEnvs((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv)
        {
            Dictionary<string, Primitive> newVarEnv = new();
            foreach (var variable in varEnv.outer)
            {
                newVarEnv.Add(variable.Key, variable.Value);
            }
        }
    }

    internal enum Operator { Plus, PlusPlus, Minus, MinusMinus, Times, Divide, SquareRoot, Floor, Ceil, GreaterThan, LessThan, GreaterEqual, LessEqual, Equals, NotEquals, Not, And, Or, Modulo }

    internal class PrimitiveExpression : Expression
    {
        Primitive _value;

        public PrimitiveExpression(Primitive value)
        {
            _value = value;
        }

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            return _value;
        }

        public override string Unparse()
        {
            return _value.ToString();
        }
    }

    internal class InitArrayExpression : Expression
    {
        private Expression _size;

        public InitArrayExpression(Expression size)
        {
            _size = size;
        }

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            var size = _size.Evaluate(varEnv, funEnv);
            return size switch
            {
                Number n => n.Value == (int)n.Value ? new Array((int)n.Value) : throw new PQLangError("Array can only be of integer size"),
            };
        }

        public override string Unparse()
        {
            return "[" + _size.ToString() + "]";
        }
    }

    internal class ArrayLookUpExpression : Expression
    {
        private string _varName;
        private Expression _index;

        public ArrayLookUpExpression(string varName, Expression index)
        {
            _varName = varName;
            _index = index;
        }

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            var array = VarEnvLookup(_varName, varEnv);
            if (!(array is Array)) throw new PQLangError(_varName + " is " + array.Type() + " and can not be accessed as an array");

            var index = _index.Evaluate(varEnv, funEnv);
            if (!(index is Number)) throw new PQLangError("Index was " + index.Type() + " and needs to be an integer");
            if (!(((Number)index).Value == (int)((Number)index).Value)) throw new PQLangError("Index was " + ((Number)index).Value + " and needs to be an integer");

            return ((Array)array).GetValue((int)((Number)index).Value);
        }

        public override string Unparse()
        {
            throw new NotImplementedException();
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

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            Primitive leftVal = _Left.Evaluate(varEnv, funEnv);
            Primitive rightVal = _right.Evaluate(varEnv, funEnv);

            return (leftVal, rightVal) switch
            {
                (Number l, Number r) => _op switch
                {
                    Operator.Plus => new Number(l.Value + r.Value),
                    Operator.Minus => new Number(l.Value - r.Value),
                    Operator.Times => new Number(l.Value * r.Value),
                    Operator.Divide => new Number(l.Value / r.Value),
                    Operator.GreaterThan => new Boolean(l.Value > r.Value),
                    Operator.LessThan => new Boolean(l.Value < r.Value),
                    Operator.Equals => new Boolean(l.Value == r.Value),
                    Operator.NotEquals => new Boolean(l.Value != r.Value),
                    Operator.Modulo => new Number(l.Value % r.Value),
                    Operator.GreaterEqual => new Boolean(l.Value >= r.Value),
                    Operator.LessEqual => new Boolean(l.Value <= r.Value),
                    _ => throw new PQLangError("Operator " + _op.ToString() + " not valid for 2 Integers")
                },

                (Boolean l, Boolean r) => _op switch
                {
                    Operator.Equals => new Boolean(l.Value == r.Value),
                    Operator.NotEquals => new Boolean(l.Value != r.Value),
                    Operator.And => new Boolean(l.Value && r.Value),
                    Operator.Or => new Boolean(l.Value || r.Value),
                    _ => throw new PQLangError("Operator " + _op.ToString() + " not valid for 2 Booleans")
                },

                //String concat
                (String l, Primitive r) => _op switch
                {
                    Operator.Plus => new String(l.Value + r.ToString()),
                    _ => throw new PQLangError("Operator " + _op.ToString() + " not valid for String and " + r.Type())
                },
                (Primitive l, String r) => _op switch
                {
                    Operator.Plus => new String(l.ToString() + r.Value),
                    _ => throw new PQLangError("Operator " + _op.ToString() + " not valid for " + l.Type() +" and String")
                },

                _ => _op switch
                {
                    Operator.Equals => new Boolean(false),
                    _ => throw new PQLangError("Operator " + _op.ToString() + " not valid for " + leftVal.Type() + " and " + rightVal.Type())
                }
            };
        }

        public override string Unparse()
        {
            return "(" + _Left.Unparse() + " " + _op.ToString() + " " + _right.Unparse() + ")";
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

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            Primitive val = _exp.Evaluate(varEnv, funEnv);

            return val switch
            {
                Number i => _op switch
                {
                    Operator.Minus => new Number(-i.Value),
                    Operator.PlusPlus => new Number(i.Value + 1),
                    Operator.MinusMinus => new Number(i.Value - 1),
                    Operator.SquareRoot => new Number((float)Math.Sqrt(i.Value)),
                    Operator.Floor => new Number((float)Math.Floor(i.Value)),
                    Operator.Ceil => new Number((float)Math.Ceiling(i.Value)),
                    _ => throw new PQLangError("Operator not valid for 1 integer")
                },

                Boolean b => _op switch
                {
                    Operator.Not => new Boolean(!b.Value),
                    _ => throw new PQLangError("Operator not valid for 1 boolean")
                },

                _ => throw new PQLangError("Not valid binary expression, type mismatch")
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

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            if (varEnv.outer.ContainsKey(_funName) || varEnv.local.ContainsKey(_funName)) throw new PQLangError("Function \"" + _funName +"\" already exists");

            funEnv.local.Add(_funName, this);

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

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            FunctionDefinitionExpression func = FunEnvLookup(_funName, funEnv);
            if (func.Arguments.Length != _arguments.Length) throw new PQLangError("Expected " + func.Arguments.Length + " arguments for function \"" + _funName +  "\" but got " + _arguments.Length);

            var newVarEnv = Copy(varEnv);

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

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
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

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            if (!varEnv.ContainsKey(_varName)) throw new PQLangError("Variable \"" + _varName + "\" does not exist");

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

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            Primitive conditionVal = _condition.Evaluate(varEnv, funEnv);

            return conditionVal switch
            {
                Boolean bo => bo.Value switch
                {
                    true => _body.Evaluate(varEnv, funEnv),
                    false => _else.Evaluate(varEnv, funEnv)
                },
                _ => throw new PQLangError("Condition was not a boolean")
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
        Expression _body;

        public WhileExpression(Expression condition, BlockExpression body)
        {
            _condition = condition;
            _body = body;
        }

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            var cVarEnv = Copy(varEnv);
            while (true)
            {
                cVarEnv = Copy(cVarEnv);
                Primitive conditionVal = _condition.Evaluate(varEnv, funEnv);

                if (!(conditionVal is Boolean)) throw new PQLangError("Condition was not a boolean");

                Boolean b = (Boolean)conditionVal;
                if (b.Value)
                {
                    _body.Evaluate(cVarEnv, funEnv);
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

        public List<FunctionDefinitionExpression> Functions { get { 
                return _body.Where(e => e is FunctionDefinitionExpression).Select(e => (FunctionDefinitionExpression)e).ToList();
            } 
        }

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            Primitive result = new Void();

            var newVarEnv = Copy(varEnv);
            var newFunEnv = Copy(funEnv);

            foreach (Expression expression in _body)
            {
                result = expression.Evaluate(newVarEnv, newFunEnv);
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

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            Console.WriteLine("PQ: " + _toPrint.Evaluate(varEnv, funEnv).ToString());

            return new Void();
        }

        public override string Unparse()
        {
            return _toPrint.Unparse();
        }
    }

    internal class ForLoopExpression : Expression
    {
        AssignmentExpression _assign;
        Expression _condition;
        AssignmentExpression _increment;
        BlockExpression _body;

        public ForLoopExpression(AssignmentExpression assign, Expression condition, AssignmentExpression increment, BlockExpression body)
        {
            _assign = assign;
            _condition = condition;
            _increment = increment;
            _body = body;
        }

        public override Primitive Evaluate((Dictionary<string, Primitive> outer, Dictionary<string, Primitive> local) varEnv, (Dictionary<string, FunctionDefinitionExpression> outer, Dictionary<string, FunctionDefinitionExpression> local) funEnv)
        {
            var newVarEnv = Copy(varEnv);
            _assign.Evaluate(newVarEnv, funEnv);
            var cVarEnv = Copy(newVarEnv);
            while (true)
            {
                cVarEnv = Copy(cVarEnv);

                Primitive conditionVal = _condition.Evaluate(cVarEnv, funEnv);

                if (!(conditionVal is Boolean)) throw new PQLangError("Condition was not a boolean");

                Boolean b = (Boolean)conditionVal;
                if (b.Value)
                {
                    _body.Evaluate(cVarEnv, funEnv);

                    _increment.Evaluate(cVarEnv, funEnv);
                }
                else return new Void();
            }
        }

        public override string Unparse()
        {
            return "for(" + _assign.Unparse() + ";" + _condition.Unparse() + ";" + _increment.Unparse() + "){" + _body.Unparse() + "}";
        }
    }
}