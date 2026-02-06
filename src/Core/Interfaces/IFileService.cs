using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    public interface IFileService
    {
        string[] GetFiles(string path);
    }
}
