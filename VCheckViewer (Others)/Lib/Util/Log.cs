using log4net;
using System.Reflection;

namespace VCheckViewer_Others.Lib.Util
{
    public interface Log
    {
        void Debug(String msg);
        void Info(String msg);
        void Error(String msg, Exception? ex = null);
    }

    public class Logger : Log
    {
        private readonly ILog _logger;

        public Logger()
        {
            this._logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        }
        public void Debug(string msg)
        {
            this._logger?.Debug(msg);
        }
        public void Info(string msg)
        {
            this._logger?.Info(msg);
        }
        public void Error(string msg, Exception? ex = null)
        {
            this._logger?.Error(msg, ex);
        }
    }
}
