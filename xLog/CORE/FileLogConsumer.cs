using System.IO;
using System.Threading;

namespace xLog
{
    public class FileLogConsumer : ILogLineConsumer
    {
        #region Properties
        string filePath;
        FileStream stream;
        StreamWriter streamWriter;
        FileMode fileMode;
        private int Disposed = 0;
        #endregion

        #region Constructor
        public FileLogConsumer(string logFile, FileMode fileMode = FileMode.Create)
        {
            this.fileMode = fileMode;
            string filePath = Path.GetFullPath(logFile);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            stream = File.Open(filePath, fileMode);
            streamWriter = new StreamWriter(stream) { AutoFlush = true };
            streamWriter.WriteLine();
        }
        #endregion

        #region Finalizers
        ~FileLogConsumer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool userInitiated)
        {
            if (Interlocked.Exchange(ref Disposed, 1) == 0)
            {
                streamWriter.Flush();// Final Flush
                streamWriter.Close();
                streamWriter.Dispose();
                streamWriter = null;

                stream.Dispose();
            }
        }
        #endregion

        public void Consume(LogLine Line)
        {
            if (Disposed == 1)
            {
                return;
            }

            streamWriter.WriteLine(Line.Text);
        }

        public void Flush()
        {
            if (Disposed == 1)
            {
                return;
            }
            streamWriter.Flush();
        }
    }
}