using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Serialization;
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
using Torch.Views;
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

        private XmlAttributeOverrides _overrides;

        private Persistent<RandomWeatherConfig> _config;
        private int _updateCounter;
        private bool _loading;
        public RandomWeatherConfig Config => _config?.Data;


        
        private UserControl _control;
        private UserControl Control => _control ?? (_control = new PropertyGrid{ DataContext = RandomWeatherConfig.Instance});
        public UserControl GetControl()
        {
            return Control;
        }
        private void EnableControl(bool enable = true)
        {
            _control?.Dispatcher?.Invoke(() =>
            {
                Control.IsEnabled = enable;
                Control.DataContext = RandomWeatherConfig.Instance;
            });

        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;
            ChatName = Torch.Config.ChatName;

            Load();

            _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += SessionChanged;
            /*
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
            */
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newstate)
        {
            switch (newstate)
            {
                case TorchSessionState.Loading:
                    break;
                case TorchSessionState.Loaded:
                    DoInit();
                    EnableControl();
                    break;
                case TorchSessionState.Unloading:
                    break;
                case TorchSessionState.Unloaded:
                    Dispose();
                    break;
                default:
                    return;
            }
        }

        #region Saving/Loading

        public void Save()
        {

            if (_loading)
                return;

            try
            {
                lock (this)
                {
                    var configFile = Path.Combine(StoragePath, "RandomWeatherPlugin.cfg");
                    using (var writer = new StreamWriter(configFile))
                    {
                        XmlSerializer x;
                        if (_overrides != null)
                            x = new XmlSerializer(typeof(RandomWeatherConfig), _overrides);
                        else
                            x = new XmlSerializer(typeof(RandomWeatherConfig));
                        x.Serialize(writer, RandomWeatherConfig.Instance);
                        writer.Close();
                    }
                    Log.Info($"Saved");

                }
            }
            catch (Exception e)
            {
                lock (this)
                {
                    Log.Error(e);
                }
            }

        }

        private void Load()
        {
            _loading = true;

            try
            {
                lock (this)
                {
                    var configFile = Path.Combine(StoragePath, "RandomWeatherPlugin.cfg");

                    if (File.Exists(configFile))
                    {
                        using (var reader = new StreamReader(configFile))
                        {
                            var x = _overrides != null ? new XmlSerializer(typeof(RandomWeatherConfig), _overrides) : new XmlSerializer(typeof(RandomWeatherConfig));
                            var settings = (RandomWeatherConfig)x.Deserialize(reader);
                            
                            reader.Close();
                            if(settings != null)RandomWeatherConfig.Instance = settings;
                        }
                    }
                    else
                    {
                        Log.Info("No settings. Initialzing new file at " + configFile);
                        RandomWeatherConfig.Instance = new RandomWeatherConfig();
                        RandomWeatherConfig.Instance.CustomWeatherRules.Add(new CustomWeatherRule());
                        using (var writer = new StreamWriter(configFile))
                        {
                            var x = _overrides != null ? new XmlSerializer(typeof(RandomWeatherConfig), _overrides) : new XmlSerializer(typeof(RandomWeatherConfig));
                            x.Serialize(writer, RandomWeatherConfig.Instance);
                            writer.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    Log.Error(ex);
                }
            }
            finally
            {
                _loading = false;
            }
        }

        #endregion


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

            var rules = new HashSet<CustomWeatherRule>();

            rules.UnionWith(RandomWeatherConfig.Instance.CustomWeatherRules);
            
            foreach (var planet in _planets)
            {
                if (!planet.HasAtmosphere) continue;
                var removeWeather = new List<string>();
                if (planet.Storage.MarkedForClose || planet.Storage == null) continue;
                CustomWeatherRule foundRule = null;

                foreach (var rule in rules)
                {
                    if (!rule.PlanetId.Equals(planet.EntityId)) continue;
                    foundRule = rule;
                    break;
                }
                var interval = foundRule?.Interval ?? RandomWeatherConfig.Instance.WeatherInterval ;

                if (!_lastRun.TryGetValue(planet, out var time))
                {
                    float planetRadius = planet.MaximumRadius;
                    Vector3D pos = planet.PositionLeftBottomCorner + new Vector3D(planetRadius, planetRadius, planetRadius);
                    var currentWeather = WeatherGenerator.GetWeather(pos);
                    MyWeatherEffectDefinition weatherToSpawn;
                    if (_lastChoice != null)removeWeather.Add(_lastChoice.Id.SubtypeName);

                    if (string.IsNullOrEmpty(currentWeather) || currentWeather.Equals("clear",StringComparison.OrdinalIgnoreCase))
                    {
                        weatherToSpawn = foundRule == null
                            ? WeatherGenerator.GetRandomWeather(removeWeather)
                            : WeatherGenerator.GetRandomWeatherFromList(foundRule.WeatherList, currentWeather);
                    }
                    else
                    {
                        weatherToSpawn = WeatherGenerator.GetWeatherDefinition("Clear");
                    }
                    if (weatherToSpawn == null)continue;
                    _lastChoice = weatherToSpawn;
                    WeatherGenerator.SetWeatherOnPlanet(planet,weatherToSpawn);
                    _lastRun[planet] = DateTime.Now;

                    break;
                }
                if (Math.Abs((DateTime.Now - time).TotalMilliseconds) < interval * 60000) continue;
                _lastRun.Remove(planet);
                break;
            }

        }



        /// <summary>
        /// Runs each time game updates
        /// </summary>
        public override void Update()
        {
            base.Update();
            if (MyAPIGateway.Session == null|| !RandomWeatherConfig.Instance.Enable || MySession.Static.Players.GetOnlinePlayerCount() == 0)
                return;

            if (++_updateCounter % 1000 == 0 && MySession.Static.Players.GetOnlinePlayerCount() > 0)
            {
                if (_planets.Count == 0)
                {
                    _planets.UnionWith(MyEntities.GetEntities().OfType<MyPlanet>().Where(x=>x.HasAtmosphere));
                    Log.Warn("No Planet Found");
                    return;
                }
                else
                {
                    SpawnWeathers();
                }
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
