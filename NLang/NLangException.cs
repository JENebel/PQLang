using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQLang
{
    public class NLangError : Exception
    {
        public override string Message { get; }

        public NLangError(string message)
        {
            Message = message;
        }
    }
}
