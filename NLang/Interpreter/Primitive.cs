using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLang.Interpreter
{
    internal abstract class Primitive
    {
        public abstract override string ToString();
    }

    internal class Integer : Primitive
    {
        public int Value { get; private set; }

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
        public bool Value { get; private set; }

        public Boolean (bool value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    internal class Unit : Primitive
    {
        public override string ToString()
        {
            return "Unit";
        }
    }
}