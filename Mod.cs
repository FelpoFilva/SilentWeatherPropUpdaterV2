// FIX: Change namespace to match project name
namespace SilentWeatherPropUpdaterV2
{
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;

    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(SilentWeatherPropUpdaterV2)}.{nameof(Mod)}")
            .SetShowsErrorsInUI(false);

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));
            updateSystem.UpdateAt<WeatherPropUpdaterSystem>(SystemUpdatePhase.GameSimulation);
            
            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
    }
}