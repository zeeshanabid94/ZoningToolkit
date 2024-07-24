using System;
using System.Runtime.CompilerServices;
using Colossal.Logging;

namespace ZoningToolkit.Utils
{
    public static class LogUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILog getLogger<T>(this T typeClass)
        {
            if (typeClass.GetType().IsClass || typeClass.GetType().IsValueType)
            {
                string nameOfType = typeClass.GetType().FullName;
                ILog logger = LogManager.GetLogger($"{nameof(ZoningToolkit)}").SetShowsErrorsInUI(true);

                // Always make effectiveness to error when publishing
                // Don't want logs to continuously dumped on user machine
                logger.SetEffectiveness(Level.Error);
                return logger;
            }
            else
            {
                throw new Exception("Logger can only be created for System.IO.Reflections.Types");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILog getLogger()
        {
            ILog logger = LogManager.GetLogger($"{nameof(ZoningToolkit)}").SetShowsErrorsInUI(true);
            // Always make effectiveness to error when publishing
            // Don't want logs to continuously dumped on user machine
            logger.SetEffectiveness(Level.Error);
            return logger;
        }
    }
}