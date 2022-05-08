using PQLang.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQLang.Interpreter
{
    internal abstract class Primitive
    {
        public abstract override string ToString();
        public abstract string Type();
        public abstract void Mutate(Primitive newValue);
        public abstract Primitive Copy();
    }

    internal class Number : Primitive
    {
        public float Value { get; set; }
        public override string Type() { return "Number"; }

        public Number(int value)
        {
            Value = value;
        }

        public Number(float value)
        {
            Value = value;
        }

        public bool IsInteger()
        {
            return Value == (int)Value;
        }
        
        public override string ToString()
        {
            return Value.ToString();
        }

        public override void Mutate(Primitive newValue)
        {
            Value = ((Number)newValue).Value;
        }

        public override Primitive Copy()
        {
            return new Number(Value);
        }
    }

    internal class Boolean : Primitive
    {
        public bool Value { get; set; }
        public override string Type() { return "Boolean"; }

        public Boolean (bool value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString().ToLower();
        }

        public override void Mutate(Primitive newValue)
        {
            Value = ((Boolean)newValue).Value;
        }

        public override Primitive Copy()
        {
            return new Boolean(Value);
        }
    }

    internal class String : Primitive
    {
        public string Value { get; set; }
        public override string Type() { return "String"; }

        public String(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }

        public override void Mutate(Primitive newValue)
        {
            Value = ((String)newValue).Value;
        }

        public override Primitive Copy()
        {
            return new String(Value);
        }
    }

    internal class Array : Primitive
    {
        public Primitive[] Values { get; set; }
        public override string Type() { return "Array"; }

        public Array(int size)
        {
            Values = new Primitive[size];
        }

        public Array(Primitive[] array)
        {
            Values = array;
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", Values.Select(x => x != null ? x.ToString() : "?")) + "]";
        }

        public override void Mutate(Primitive newValue)
        {
            Values = ((Array)newValue).Values;
        }

        public void Mutate(Primitive newValue, int index)
        {
            Values[index] = newValue.Copy();
        }

        public override Primitive Copy()
        {
            return new Array(Values.Length) { Values = Values.Select(x => x != null ? x.Copy() : new Void()).ToArray() };
        }
    }

    internal class Void : Primitive
    {
        public override string Type() { return "Void"; }

        public override string ToString()
        {
            return "void";
        }

        public override void Mutate(Primitive newValue)
        {
            throw new PQLangError("Not possible to mutate \"Void\". What are you doing?");
        }

        public override Primitive Copy()
        {
            return new Void();
        }
    }

    internal class Break : Primitive
    {
        public override string Type() { return "Break"; }

        public override string ToString()
        {
            return "break";
        }

        public override void Mutate(Primitive newValue)
        {
            throw new PQLangParseError("Not possible to mutate \"Break\". What are you doing?");
        }

        public override Primitive Copy()
        {
            return new Break();
        }
    }

    internal class Return : Primitive
    {
        public override string Type() { return "Return"; }
        public Expression Value { get; }

        public Return(Expression value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return "return " + Value.ToString();
        }

        public override void Mutate(Primitive newValue)
        {
            throw new PQLangParseError("Not possible to mutate \"Return\". What are you doing?");
        }

        public override Primitive Copy()
        {
            throw new NotImplementedException();
        }
    }

    internal class Object : Primitive
    {
        public Dictionary<string, Primitive> VarEnv { get; set; }
        public Dictionary<string, FunctionDefinitionExpression> FunEnv { get; set; }
        public Dictionary<string, ClassDefinitionExpression> ClassEnv { get; set; }
        public string ClassName { get; set; }

        public Object(string className, Dictionary<string, Primitive> varEnv, Dictionary<string, FunctionDefinitionExpression> funEnv, Dictionary<string, ClassDefinitionExpression> classEnv)
        {
            VarEnv = varEnv;
            FunEnv = funEnv;
            ClassEnv = classEnv;
            ClassName = className;
        }

        public override Primitive Copy()
        {
            return new Object(ClassName, Expression.Copy(VarEnv), FunEnv, ClassEnv);
        }

        public override void Mutate(Primitive newValue)
        {
            VarEnv = Expression.Copy(((Object)newValue).VarEnv);
            FunEnv = Expression.Copy(((Object)newValue).FunEnv);
            ClassEnv = Expression.Copy(((Object)newValue).ClassEnv);
            ClassName = ((Object)newValue).ClassName;
        }

        public override string ToString()
        {
            return Type();
        }

        public override string Type()
        {
            return "<" + ClassName + ">";
        }
    }
}