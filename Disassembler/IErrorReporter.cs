using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Disassembler
{
    public interface IErrorReporter
    {
        void AddErrorMessage(string message);
    }
}
