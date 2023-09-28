using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Security;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;


namespace MyLogger
{
    public class MLogger : Logger, ILogger
    {
        private StreamWriter _streamWriter;
        private int _indent;

        public string PathToLogFile { get; set; }

        public MLogger (string pathToLogFile)
        {
            PathToLogFile = pathToLogFile;
        }

        public MLogger() {}

        public override void Initialize(IEventSource eventSource)
        {

            if (String.IsNullOrEmpty(Parameters))
                throw new LoggerException("Log file wasn't set.");

            try
            {
                // Open the file
                _streamWriter = new StreamWriter(PathToLogFile, true);
            }
            catch(Exception ex)
            {
                if
                (
                    ex is UnauthorizedAccessException
                    || ex is ArgumentNullException
                    || ex is PathTooLongException
                    || ex is DirectoryNotFoundException
                    || ex is NotSupportedException
                    || ex is ArgumentException
                    || ex is SecurityException
                    || ex is IOException
                )
                {
                    throw new LoggerException("Failed to create log file: " + ex.Message);
                }
                else
                {
                    // Unexpected failure
                    throw;
                }
            }

            // For brevity, we'll only register for certain event types. Loggers can also
            // register to handle TargetStarted/Finished and other events.
            eventSource.ProjectStarted += new ProjectStartedEventHandler(eventSource_ProjectStarted);
            eventSource.TaskStarted += new TaskStartedEventHandler(eventSource_TaskStarted);
            eventSource.MessageRaised += new BuildMessageEventHandler(eventSource_MessageRaised);
            eventSource.WarningRaised += new BuildWarningEventHandler(eventSource_WarningRaised);
            eventSource.ErrorRaised += new BuildErrorEventHandler(eventSource_ErrorRaised);
            eventSource.ProjectFinished += new ProjectFinishedEventHandler(eventSource_ProjectFinished);
        }

        private void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            // BuildErrorEventArgs adds LineNumber, ColumnNumber, File, amongst other parameters
            string line = $": ERROR {e.File}({e.LineNumber},{e.ColumnNumber}): ";
            WriteLineWithSenderAndMessage(line, e);
        }

        void eventSource_WarningRaised(object sender, BuildWarningEventArgs e)
        {
            string line = $": ERROR {e.File}({e.LineNumber},{e.ColumnNumber}): ";
            WriteLineWithSenderAndMessage(line, e);
        }

        void eventSource_MessageRaised(object sender, BuildMessageEventArgs e)
        {
            // BuildMessageEventArgs adds Importance to BuildEventArgs
            // Take account of the verbosity setting we've been passed in deciding whether to log the message
            if ((e.Importance == MessageImportance.High && IsVerbosityAtLeast(LoggerVerbosity.Minimal))
                || (e.Importance == MessageImportance.Normal && IsVerbosityAtLeast(LoggerVerbosity.Normal))
                || (e.Importance == MessageImportance.Low && IsVerbosityAtLeast(LoggerVerbosity.Detailed))
                )
            {
                WriteLineWithSenderAndMessage(String.Empty, e);
            }
        }

        void eventSource_TaskStarted(object sender, TaskStartedEventArgs e)
        {
            // TaskStartedEventArgs adds ProjectFile, TaskFile, TaskName
            // To keep this log clean, this logger will ignore these events.
        }

        void eventSource_ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            // ProjectStartedEventArgs adds ProjectFile, TargetNames
            // Just the regular message string is good enough here.
            WriteLine(String.Empty, e);
            _indent++;
        }

        void eventSource_ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            // The regular message string is good enough here too.
            _indent--;
            WriteLine(String.Empty, e);
        }

        // Write a line to the log, adding the SenderName and Message
        private void WriteLineWithSenderAndMessage(string line, BuildEventArgs e)
        {
            if (0 == String.Compare(e.SenderName, "MSBuild", true /*ignore case*/))
            {
                // If the sender name is MSBuild, leave it out for prettiness
                WriteLine(line, e);
            }
            else
            {
                WriteLine(e.SenderName + ": " + line, e);
            }
        }

        // Just write a line to the log
        private void WriteLine(string line, BuildEventArgs e)
        {
            for (int i = _indent; i > 0; i--)
            {
                _streamWriter.Write("\t");
            }
            _streamWriter.WriteLine($"Time: {DateTime.Now}  " + line + e.Message);
        }

        // Shutdown() is guaranteed to be called by MSBuild at the end of the build, after all
        // events have been raised.
        public override void Shutdown()
        {
            // Done logging
            _streamWriter.Close();
        }

    }
}
