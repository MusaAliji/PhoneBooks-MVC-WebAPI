using PhoneBooks.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Web;

namespace PhoneBooks.Custom
{
    public class PhoneBookHelper
    {
        private static readonly Object LockWriteFile = new Object();
        public bool FileExists(string physicalPath) => System.IO.File.Exists(Path.Combine(physicalPath));
        public long FileLength(string physicalPath) => new FileInfo(physicalPath).Length;

        public void AppendLine(string filePath, StringBuilder builder, bool isHeader)
        {
            bool WriteLocked = false;

            try
            {
                Monitor.Enter(LockWriteFile, ref WriteLocked);

                if (isHeader)
                {
                    using (StreamWriter sw = new StreamWriter(filePath, true))
                    {
                        sw.Write(builder.ToString());
                    }
                }
                else
                    File.AppendAllText(filePath, builder.ToString());

            }
            finally
            {
                if (WriteLocked)
                    Monitor.Exit(LockWriteFile);
            }
        }
    }
}