using Log.Enums;
using Log.Interfaces;
using Log.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Log.Factory
{
    internal class LogWriterFactory
    {
        public static ILogWriter Create(LogFormat format, string folder)
        {
            return format switch
            {
                LogFormat.Xml => new XmlLogWriter(folder),
                _ => new JsonLogWriter(folder) // JSON par défaut
            };
        }
    }
}
