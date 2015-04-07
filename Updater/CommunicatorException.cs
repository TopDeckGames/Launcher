using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    public class CommunicatorException : Exception
    {
        public CommunicatorException(string message)
            : base(message)
        {
        }
    }
}
