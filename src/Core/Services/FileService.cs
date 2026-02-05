using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public class FileService : IFileService
    {
        public FileService() { }
        public string[] GetFiles(string path)
        {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        }
    }

   
}
