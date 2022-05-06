using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQLang.Errors
{
    public class NLangParseError : Exception
    {
        public override string Message { get; }

        public NLangParseError(string message)
        {
            Message = message;
        }
    }
}