using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQLang.Errors
{
    public class PQError : Exception
    {
        public override string Message { get; }

        public PQError(string message)
        {
            Message = message;
        }
    }
}
