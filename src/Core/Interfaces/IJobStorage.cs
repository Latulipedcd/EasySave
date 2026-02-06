using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    public interface IJobStorage
    {
        string JobsDirectory { get; }
        string JobsFilePath { get; }
    }
}
