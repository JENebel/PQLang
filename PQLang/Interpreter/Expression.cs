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
        public abstract Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv);

        public abstract string Unparse();

        public static Dictionary<string, T> Copy<T>(Dictionary<string, T> env) { return new Dictionary<string, T>(env); }
    }

    internal enum Operator { Plus, PlusPlus, Minus, MinusMinus, Times, Divide, SquareRoot, Floor, Ceil, GreaterThan, LessThan, GreaterEqual, LessEqual, Equals, NotEquals, Not, And, Or, Modulo }

    internal class PrimitiveExpression : Expression
    {
        Primitive _value;

        public PrimitiveExpression(Primitive value)
        {
            _value = value;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            var size = _size.Evaluate(varEnv, funEnv, classEnv);
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            var array = varEnv[_varName];
            if (!(array is Array)) throw new PQLangError(_varName + " is " + array.Type() + " and can not be accessed as an array");

            var indexPrim = _index.Evaluate(varEnv, funEnv, classEnv);
            if (!(indexPrim is Number)) throw new PQLangError("Index was " + indexPrim.Type() + " and has to be a number");

            if (((Number)indexPrim).Value % 1 != 0) throw new PQLangError("Index was " + ((Number)indexPrim).Value + " and has to be an integer");

            int index = (int)((Number)indexPrim).Value;
            if (index >= 0 && index < ((Array)array).Values.Length)
                return ((Array)array).Values[index] == null ? new Void() : ((Array)array).Values[index];
            else
                throw new PQLangError("Index " + index + " was out of bounds");
        }

        public override string Unparse()
        {
            return _varName + "[" + _index.Unparse() + "]";
        }
    }

    internal class ArraySetExpression : Expression
    {
        private string _varName;
        private Expression _index;
        private Expression _newVal;

        public ArraySetExpression(string varName, Expression index, Expression newVal)
        {
            _varName = varName;
            _index = index;
            _newVal = newVal;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            var array = varEnv[_varName];
            if (!(array is Array)) throw new PQLangError(_varName + " is " + array.Type() + " and can not be accessed as an array");

            var indexPrim = _index.Evaluate(varEnv, funEnv, classEnv);
            if (!(indexPrim is Number)) throw new PQLangError("Index was " + indexPrim.Type() + " and has to be a number");

            if (((Number)indexPrim).Value % 1 != 0) throw new PQLangError("Index was " + ((Number)indexPrim).Value + " and has to be an integer");

            int index = (int)((Number)indexPrim).Value;

            var newVal = _newVal.Evaluate(varEnv, funEnv, classEnv);

            if (index >= 0 && index < ((Array)array).Values.Length)
            {
                if (((Array)array).Values[index] == null || ((Array)array).Values[index].GetType() != newVal.GetType()) ((Array)array).Values[index] = newVal.Copy();
                else ((Array)array).Values[index].Mutate(newVal);
            }
            else
                throw new PQLangError("Index " + index + " was out of bounds");

            return new Void();
        }

        public override string Unparse()
        {
            return _varName + "[" + _index.Unparse() + "]";
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            Primitive leftVal = _Left.Evaluate(varEnv, funEnv, classEnv);
            Primitive rightVal = _right.Evaluate(varEnv, funEnv, classEnv);

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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            Primitive val = _exp.Evaluate(varEnv, funEnv, classEnv);

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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            if (funEnv.ContainsKey(_funName)) throw new PQLangError("Function \"" + _funName +"\" already exists");

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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            if (!funEnv.ContainsKey(_funName)) throw new PQLangError("No such function \"" + _funName + "\"");
            FunctionDefinitionExpression func = funEnv[_funName];
            if (func.Arguments.Length != _arguments.Length) throw new PQLangError("Expected " + func.Arguments.Length + " arguments for function \"" + _funName +  "\" but got " + _arguments.Length);

            var newVarEnv = Copy(varEnv);

            for (int i = 0; i < _arguments.Count(); i++)
            {
                newVarEnv[func.Arguments[i]] = _arguments[i].Evaluate(varEnv, funEnv, classEnv);
            }

            var result = func.Body.Evaluate(newVarEnv, funEnv, classEnv);
            if (result is Return) result = ((Return)result).Value.Evaluate(varEnv, funEnv, classEnv);
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            Primitive newVal = _body.Evaluate(varEnv, funEnv, classEnv);

            if (varEnv.ContainsKey(_varName))
            {
                if (varEnv[_varName].GetType() != newVal.GetType()) varEnv[_varName] = newVal.Copy();
                else varEnv[_varName].Mutate(newVal);
            }
            else
                varEnv.Add(_varName, newVal.Copy());

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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            Primitive conditionVal = _condition.Evaluate(varEnv, funEnv, classEnv);

            return conditionVal switch
            {
                Boolean bo => bo.Value switch
                {
                    true => _body.Evaluate(varEnv, funEnv, classEnv),
                    false => _else.Evaluate(varEnv, funEnv, classEnv)
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            var cVarEnv = Copy(varEnv);
            while (true)
            {
                cVarEnv = Copy(cVarEnv);
                Primitive conditionVal = _condition.Evaluate(varEnv, funEnv, classEnv);

                if (!(conditionVal is Boolean)) throw new PQLangError("Condition was not a boolean");

                Boolean b = (Boolean)conditionVal;
                if (b.Value)
                {
                    var res = _body.Evaluate(cVarEnv, funEnv, classEnv);
                    if (res is Break) return new Void();
                    if (res is Return) return res;
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
        List<Expression> _body;

        public BlockExpression(List<Expression> body)
        {
            _body = body;
        }

        public List<FunctionDefinitionExpression> Functions 
        { 
            get 
            { 
                return _body.Where(e => e is FunctionDefinitionExpression).Select(e => (FunctionDefinitionExpression)e).ToList();
            } 
        }

        public List<ClassDefinitionExpression> Classes
        {
            get
            {
                return _body.Where(e => e is ClassDefinitionExpression).Select(e => (ClassDefinitionExpression)e).ToList();
            }
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            return Evaluate(varEnv, funEnv, classEnv, true);
        }

        public Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv, bool newEnvs)
        {
            Primitive result = new Void();

            var newVarEnv = varEnv;
            var newFunEnv = funEnv;
            var newClassEnv = classEnv;

            if (newEnvs)
            {
                newVarEnv = Copy(varEnv);
                newFunEnv = Copy(funEnv);
                newClassEnv = Copy(classEnv);
            }
            

            Dictionary<string, ClassDefinitionExpression> classDefs = new();

            while (_body.Count() > 0 && _body.First() is ClassDefinitionExpression)
            {
                _body.First().Evaluate(newVarEnv, newFunEnv, newClassEnv);
                classDefs.Add(((ClassDefinitionExpression)_body.First()).ClassName, (ClassDefinitionExpression)_body.First());
                _body.RemoveAt(0);
            }
            foreach (var def in classDefs.Values)
            {
                def.ClassEnv = classDefs;
            }

            foreach (Expression expression in _body)
            {
                result = expression.Evaluate(newVarEnv, newFunEnv, newClassEnv);
                if (result is Return) 
                    return new Return(new PrimitiveExpression(((Return)result).Value.Evaluate(varEnv, funEnv, classEnv).Copy()));
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            Console.WriteLine(_toPrint.Evaluate(varEnv, funEnv, classEnv).ToString());

            return new Void();
        }

        public override string Unparse()
        {
            return "print(" + _toPrint.Unparse() + ");";
        }
    }

    internal class ReadExpression : Expression
    {
        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            string str = Console.ReadLine();

            return new String(str == null ? "" : str);
        }

        public override string Unparse()
        {
            return "read";
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

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            var newVarEnv = Copy(varEnv);
            _assign.Evaluate(newVarEnv, funEnv, classEnv);
            var cVarEnv = Copy(newVarEnv);
            while (true)
            {
                cVarEnv = Copy(cVarEnv);

                Primitive conditionVal = _condition.Evaluate(cVarEnv, funEnv, classEnv);

                if (!(conditionVal is Boolean)) throw new PQLangError("Condition was not a boolean");

                Boolean b = (Boolean)conditionVal;
                if (b.Value)
                {
                    var res = _body.Evaluate(cVarEnv, funEnv, classEnv);

                    if (res is Break) return new Void();
                    if (res is Return) return res;

                    _increment.Evaluate(cVarEnv, funEnv, classEnv);
                }
                else return new Void();
            }
        }

        public override string Unparse()
        {
            return "for(" + _assign.Unparse() + ";" + _condition.Unparse() + ";" + _increment.Unparse() + "){" + _body.Unparse() + "}";
        }
    }

    internal class ClassDefinitionExpression : Expression
    {
        public string ClassName { get; set; }
        public BlockExpression Body { get; private set; }
        public string[] Arguments { get; }
        public Dictionary<string, ClassDefinitionExpression> ClassEnv { get; set;}

        public ClassDefinitionExpression(string name, string[] arguments, BlockExpression body)
        {
            ClassName = name;
            Body = body;
            Arguments = arguments;
            ClassEnv = new();
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            if (classEnv.ContainsKey(ClassName)) throw new PQLangError("Class \"" + ClassName + "\" already exists");

            classEnv.Add(ClassName, this);

            return new Void();
        }

        public override string Unparse()
        {
            return "Class " + ClassName + "{" + Body.Unparse(); 
        }
    }

    internal class ClassInstantiateExpression : Expression
    {
        private string _className;
        Expression[] _arguments;

        public ClassInstantiateExpression(string name, Expression[] arguments)
        {
            _className = name;
            _arguments = arguments;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            if (!classEnv.ContainsKey(_className)) throw new PQLangError("No such class \"" + _className + "\"");
            ClassDefinitionExpression klass = classEnv[_className];
            if (klass.Arguments.Length != _arguments.Length) throw new PQLangError("Expected " + klass.Arguments.Length + " arguments for constructor for \"" + _className + "\" but got " + _arguments.Length);

            Dictionary<string, Primitive> newVarEnv = new();
            Dictionary<string, FunctionDefinitionExpression> newFunEnv = new();

            for (int i = 0; i < _arguments.Count(); i++)
            {
                newVarEnv[klass.Arguments[i]] = _arguments[i].Evaluate(newVarEnv, newFunEnv, klass.ClassEnv);
            }

            klass.Body.Evaluate(newVarEnv, newFunEnv, klass.ClassEnv, false);

            return new Object(_className, newVarEnv, newFunEnv, klass.ClassEnv);
        }

        public override string Unparse()
        {
            return "new " + _className + "(" + string.Join(',', _arguments.Select(exp => exp.Unparse())) + ")";
        }
    }

    internal class ClassSetFieldExpression : Expression
    {
        Expression _objectExp;
        string _fieldName;
        Expression _newVal;

        public ClassSetFieldExpression(Expression objectExp, string fieldName, Expression newVal)
        {
            _objectExp = objectExp;
            _fieldName = fieldName;
            _newVal = newVal;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            Primitive rawObj = _objectExp.Evaluate(varEnv, funEnv, classEnv);
            if (!(rawObj is Object)) throw new PQLangError("Can only acces fields on objects. Got " + rawObj.Type());
            Object obj = (Object)rawObj;

            AssignmentExpression ass = new AssignmentExpression(_fieldName, _newVal);
            ass.Evaluate(obj.VarEnv, obj.FunEnv, obj.ClassEnv);

            return new Void();
        }

        public override string Unparse()
        {
            return _objectExp.Unparse() + "." + _fieldName + "=" + _newVal.Unparse();
        }
    }

    internal class ClassGetFieldExpression : Expression
    {
        Expression _objectExp;
        string _fieldName;

        public ClassGetFieldExpression(Expression objectExp, string fieldName)
        {
            _objectExp = objectExp;
            _fieldName = fieldName;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            Primitive rawObj = _objectExp.Evaluate(varEnv, funEnv, classEnv);

            //Special cases
            if (_fieldName == "type") return new String(rawObj.Type());
            if (rawObj is Array && _fieldName == "length") return new Number(((Array)rawObj).Values.Length);
            if (rawObj is String && _fieldName == "length") return new Number(((String)rawObj).Value.Length);


            if (rawObj is not Object) throw new PQLangError("Can only acces fields on objects. Got " + rawObj.Type());
            Object obj = (Object)rawObj;

            VariableLookupExpression look = new VariableLookupExpression(_fieldName);
            return look.Evaluate(obj.VarEnv, obj.FunEnv, obj.ClassEnv);
        }

        public override string Unparse()
        {
            return _objectExp.Unparse() + _fieldName;
        }
    }

    internal class ClassCallMethodExpression : Expression
    {
        string _funName;
        Expression _objectExp;
        Expression[] _arguments;

        public ClassCallMethodExpression(Expression objectExp, string funName, Expression[] arguments)
        {
            _objectExp = objectExp;
            _funName = funName;
            _arguments = arguments;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            Primitive rawObj = _objectExp.Evaluate(varEnv, funEnv, classEnv);
            if (!(rawObj is Object)) throw new PQLangError("Can only acces methods on objects. Got " + rawObj.Type());
            Object obj = (Object)rawObj;

            if (!obj.FunEnv.ContainsKey(_funName)) throw new PQLangError(obj.ClassName + " Does not contain method \"" + _funName + "\"");
            FunctionDefinitionExpression func = obj.FunEnv[_funName];
            if (func.Arguments.Length != _arguments.Length) throw new PQLangError("Expected " + func.Arguments.Length + " arguments for method \"" + _funName + "\" but got " + _arguments.Length);

            var newVarEnv = obj.VarEnv;

            for (int i = 0; i < _arguments.Count(); i++)
            {
                newVarEnv[func.Arguments[i]] = _arguments[i].Evaluate(varEnv, funEnv, classEnv);
            }

            var result = func.Body.Evaluate(newVarEnv, obj.FunEnv, obj.ClassEnv);
            if (result is Return) result = ((Return)result).Value.Evaluate(varEnv, funEnv, classEnv);
            return result;
        }

        public override string Unparse()
        {
            return _objectExp.Unparse() + "(" + string.Join(',', _arguments.Select(exp => exp.Unparse())) + ")";
        }
    }

    internal class ErrorExpression : Expression
    {
        Expression _message;

        public ErrorExpression(Expression message)
        {
            _message = message;
        }

        public override Primitive Evaluate(Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            throw new PQError(_message.Evaluate(varEnv, funEnv, classEnv).ToString());
        }

        public override string Unparse()
        {
            return "error(" + _message.Unparse() + ");";
        }
    }
}