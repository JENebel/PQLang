using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQLang.Errors
{
    public class PQLangParseError : Exception
    {
        public override string Message { get; }

        public PQLangParseError(string message)
        {
            Message = message;
        }
    }
}