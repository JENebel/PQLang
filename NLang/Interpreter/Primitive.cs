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

    internal class Integer : Primitive
    {
        public int Value { get; }
        public override string Type() { return "Integer"; }

        public Integer(int value)
        {
            Value = value;
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

    internal class Void : Primitive
    {
        public override string Type() { return "Void"; }

        public override string ToString()
        {
            return "void";
        }
    }
}