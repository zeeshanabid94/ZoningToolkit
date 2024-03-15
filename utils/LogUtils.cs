using System;
using Colossal.Logging;

namespace ZoningToolkit.Utilties
{
    public static class LogUtils
    {
        public static ILog getLogger<T>(this T typeClass)
        {
            if (typeClass.GetType().IsClass || typeClass.GetType().IsValueType)
            {
                string nameOfType = typeClass.GetType().FullName;
                ILog logger = LogManager.GetLogger($"{nameof(ZoningToolkit)}.{nameOfType}").SetShowsErrorsInUI(false);
                return logger;
            }
            else
            {
                throw new Exception("Logger can only be created for System.IO.Reflections.Types");
            }
        }
    }
}