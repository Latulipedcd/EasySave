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
        public static ILogWriter Create(LogFormat format)
        {
            return format switch
            {
                LogFormat.Xml => new XmlLogWriter(),
                _ => new JsonLogWriter() // JSON par défaut
            };
        }
    }
}
