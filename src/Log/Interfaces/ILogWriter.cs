using System.Text.Json;
using System.Text.Json.Nodes;

namespace Log.Interfaces
{
    internal interface ILogWriter
    {
        
        void Write(Object entry);
    }
}

