using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSCSDK
{
    public class TscException : Exception
    {
        public TscException(string message) : base(message)
        {
        }
    }
}
