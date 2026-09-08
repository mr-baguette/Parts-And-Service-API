#define BEPINEX
#if BEPINEX
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using PnSAPI.Core;
using LogLevel = PnSAPI.Core.LogLevel;

namespace PnSAPI.Adapters
{
    [BepInPlugin("com.oui_baguette1.PnSAPI", "Parts And Services API", "0.0.1")]
    internal class BepInExAdapter : BasePlugin
    {
        private static readonly Dictionary<string, ManualLogSource> Loggers = new();
        private static ManualLogSource DefaultLogger;

        public override void Load()
        {
            DefaultLogger = Log;
            PnSAPIBridge.LogHandler = HandleLog;
            PnSAPIBridge.Initialize(Paths.GameRootPath, Paths.ConfigPath);
        }

        private void HandleLog(string message, string source, LogLevel level)
        {
            if (!Loggers.TryGetValue(source, out ManualLogSource logger))
            {
                logger = BepInEx.Logging.Logger.CreateLogSource(source);
                Loggers[source] = logger;
            }

            switch (level)
            {
                case LogLevel.Debug: logger.LogDebug(message); break;
                case LogLevel.Info: logger.LogInfo(message); break;
                case LogLevel.Warning: logger.LogWarning(message); break;
                case LogLevel.Error: logger.LogError(message); break;
            }
        }
    }
}
#endif
