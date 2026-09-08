#if MELONLOADER
using System.IO;
using MelonLoader;
using PnSAPI.Adapters;
using PnSAPI.Core;

[assembly: MelonInfo(typeof(MelonAdapter), "Parts And Services API", "0.0.1", "oui_baguette1")]
[assembly: MelonGame(null, null)]

namespace PnSAPI.Adapters
{
    internal class MelonAdapter : MelonMod
    {
        public override void OnInitializeMelon()
        {
            // Bind the logging delegate directly without Reflection
            PnSAPIBridge.LogHandler = HandleLog;

            // Resolve MelonLoader directories
            string gameRoot = MelonUtils.GameDirectory;
            string configFolder = Path.Combine(MelonUtils.UserDataDirectory, "PnSAPI");

            // Hand off execution to the cross-loader bridge
            PnSAPIBridge.Initialize(gameRoot, configFolder);
        }

        private void HandleLog(string message, string source, LogLevel level)
        {
            string formattedMsg = $"[{source}] {message}";

            switch (level)
            {
                case LogLevel.Debug: MelonLogger.Msg(System.ConsoleColor.Gray, formattedMsg); break;
                case LogLevel.Info: MelonLogger.Msg(formattedMsg); break;
                case LogLevel.Warning: MelonLogger.Warning(formattedMsg); break;
                case LogLevel.Error: MelonLogger.Error(formattedMsg); break;
            }
        }
    }
}
#endif