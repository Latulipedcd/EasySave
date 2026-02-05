using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    public interface ICopyService
    {
        bool CopyFiles(string file, string destination);
    }
}
