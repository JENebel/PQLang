using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQLang.Errors
{
    public class PQLangError : Exception
    {
        public override string Message { get; }

        public PQLangError(string message)
        {
            Message = message;
        }
    }
}
