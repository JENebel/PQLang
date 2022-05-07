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
        public abstract Primitive Copy ();
    }

    internal class Number : Primitive
    {
        public float Value { get; set; }
        public override string Type() { return "Integer"; }

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
            if (!(newValue.GetType() == GetType())) throw new PQLangError("Type mismatch. Expected " + Type() + " but got " + newValue.Type());
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
            if (!(newValue.GetType() == GetType())) throw new PQLangError("Type mismatch. Expected " + Type() + " but got " + newValue.Type());
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
            if (!(newValue.GetType() == GetType())) throw new PQLangError("Type mismatch. Expected " + Type() + " but got " + newValue.Type());
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

        public Primitive GetValue(int index) {
            if (index > 0 && index < Values.Length)
                return Values[index] == null ? new Void() : Values[index];
            else throw new PQLangError("Index " + index + " was out of bounds");
        }

        public void SetValue(int index, Primitive value) 
        {
            if (index > 0 && index < Values.Length)
                Values[index] = value;
            else throw new PQLangError("Index " + index + " was out of bounds");
        }

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
            return "(" + string.Join(", ", Values.Select(x => x.ToString())) + ")";
        }

        public override void Mutate(Primitive newValue)
        {
            if (!(newValue.GetType() == GetType())) throw new PQLangError("Type mismatch. Expected " + Type() + " but got " + newValue.Type());
            Values = ((Array)newValue).Values;
        }

        public override Primitive Copy()
        {
            throw new NotImplementedException();
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
            if (!(newValue.GetType() == GetType())) throw new PQLangError("Type mismatch. Expected " + Type() + " but got " + newValue.Type());
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
}