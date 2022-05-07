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
    }

    internal class Number : Primitive
    {
        public float Value { get; }
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
    }

    internal class Boolean : Primitive
    {
        public bool Value { get; }
        public override string Type() { return "Boolean"; }

        public Boolean (bool value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString().ToLower();
        }
    }

    internal class String : Primitive
    {
        public string Value { get; }
        public override string Type() { return "String"; }

        public String(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }
    }

    internal class Array : Primitive
    {
        public Primitive[] Values { get; }
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

        public override string ToString()
        {
            return "(" + string.Join(", ", Values.Select(x => x.ToString())) + ")";
        }
    }

    internal class Void : Primitive
    {
        public override string Type() { return "Void"; }

        public override string ToString()
        {
            return "void";
        }
    }
}