using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using NLog;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers;
using Torch.Session;
using VRage.Collections;
using VRage.Game;
using VRage.Network;
using VRageMath;

namespace RandomWeatherPlugin
{
    public class RandomWeatherPluginCore:TorchPluginBase,IWpfPlugin
    {
        public static Logger Log = LogManager.GetLogger("RandomWeatherPlugin");
        public static RandomWeatherPluginCore Instance { get; private set; }
        private TorchSessionManager _sessionManager;
        private HashSet<MyPlanet> _planets = new HashSet<MyPlanet>();
        private MyWeatherEffectDefinition _lastChoice;
        private ConcurrentDictionary<MyPlanet,DateTime>_lastRun = new ConcurrentDictionary<MyPlanet, DateTime>();
        public static string ChatName;

        private Control _control;
        public UserControl GetControl() => _control ?? (_control = new Control(this));
        private Persistent<RandomWeatherConfig> _config;
        private int _updateCounter;
        public RandomWeatherConfig Config => _config?.Data;

        public void Save() => _config.Save();

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;
            ChatName = Torch.Config.ChatName;

            _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += SessionChanged;
            var configFile = Path.Combine(StoragePath, "RandomWeatherPlugin.cfg");

            try 
            {

                _config = Persistent<RandomWeatherConfig>.Load(configFile);

            }
            catch (Exception e) 
            {
                Log.Warn(e);
            }

            if (_config?.Data != null) return;
            Log.Info("Created Default Config, because none was found!");

            _config = new Persistent<RandomWeatherConfig>(configFile, new RandomWeatherConfig());
            _config.Save();

        }

        private void SessionChanged(ITorchSession session, TorchSessionState newstate)
        {
            switch (newstate)
            {
                case TorchSessionState.Loading:
                    break;
                case TorchSessionState.Loaded:
                    DoInit();
                    break;
                case TorchSessionState.Unloading:
                    break;
                case TorchSessionState.Unloaded:
                    break;
                default:
                    return;
            }
        }

        private void DoInit()
        {
            _planets.Clear();
            _planets.UnionWith(MyEntities.GetEntities().OfType<MyPlanet>().Where(x=>x.HasAtmosphere));
            Log.Info($"Fount {_planets.Count} with atmosphere to work with this plugin");
            ListReader<MyWeatherEffectDefinition> weatherDefinitions = MyDefinitionManager.Static.GetWeatherDefinitions();

            Log.Info($"{weatherDefinitions.Count} weather definitions found on server");
            
        }


        private void SpawnWeathers()
        {
            if (MySession.Static.Players.GetOnlinePlayerCount() == 0 || _planets.Count == 0) return;
            foreach (var planet in _planets)
            {
                if (Config.ExceptedPlanets.Contains(planet.DisplayName,StringComparer.OrdinalIgnoreCase) ||planet.Storage.MarkedForClose || planet.Storage == null) continue;
                if (!planet.HasAtmosphere) continue;
                if (!_lastRun.TryGetValue(planet, out var time))
                {
                    float planetRadius = planet.MaximumRadius;
                    Vector3D pos = planet.PositionLeftBottomCorner + new Vector3D(planetRadius, planetRadius, planetRadius);
                    var removeWeather = new List<string>();
                    var currentWeather = WeatherGenerator.GetWeather(pos);
                    if (Instance.Config.ExceptedWeathers.Count>0)removeWeather.AddRange(Instance.Config.ExceptedWeathers);
                    if (currentWeather != null)removeWeather.Add(currentWeather);
                    if (_lastChoice != null)removeWeather.Add(_lastChoice.Id.SubtypeName);
                    var weatherToSpawn = WeatherGenerator.GetRandomWeather(removeWeather);
                    if (weatherToSpawn == null)continue;
                    _lastChoice = weatherToSpawn;
                    WeatherGenerator.SetWeatherOnPlanet(planet,weatherToSpawn);
                    _lastRun[planet] = DateTime.Now;

                    break;
                }
                if (Math.Abs((DateTime.Now - time).TotalMilliseconds) < Config.WeatherInterval * 60000) continue;
                _lastRun.Remove(planet);
            }

        }



        /// <summary>
        /// Runs each time game updates
        /// </summary>
        public override void Update()
        {
            base.Update();
            if (MyAPIGateway.Session == null|| !Config.Enable)
                return;
            if (++_updateCounter % 1000 == 0 && MySession.Static.Players.GetOnlinePlayerCount() > 0)
            {
                if (_planets.Count == 0)
                {
                    _planets.UnionWith(MyEntities.GetEntities().OfType<MyPlanet>().Where(x=>x.HasAtmosphere));
                    Log.Warn("No Planet Found");
                    return;
                }
                SpawnWeathers();
            }

        }

        /// <summary>
        /// Dumps everything on server closure
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            _planets.Clear();
            _lastRun.Clear();
            _lastChoice = null;

        }

    }
}
