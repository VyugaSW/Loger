using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MyLogger;
using System.Security;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Loger
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MLogger myLogger = new MLogger(@"C:\Test");
            IEventSource eventSource = null;
            myLogger.Initialize(eventSource);

        }
    }
}
