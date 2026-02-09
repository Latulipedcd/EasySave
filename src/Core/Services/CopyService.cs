using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public class CopyService : ICopyService
    {
        public CopyService() { }

        public bool CopyFiles(string file, string target) //Return boolean to know if any errors happened
        {
            try
            {
                File.Copy(file, target, true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
