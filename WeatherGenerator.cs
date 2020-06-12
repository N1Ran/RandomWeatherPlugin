using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using NLog;
using Sandbox.Game;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Torch.Managers;
using Torch.Utils;
using VRage.Game;
using VRage;
using VRage.Collections;
using VRage.Network;
using VRageMath;

namespace RandomWeatherPlugin
{
    public static class WeatherGenerator
    {

        private static Logger Log = RandomWeatherPluginCore.Log;

        public static MyWeatherEffectDefinition GetRandomWeather(List<string> exceptedList = null)
        {
            ListReader<MyWeatherEffectDefinition> weatherDefinitions = MyDefinitionManager.Static.GetWeatherDefinitions();
            var newWeatherList = new List<MyWeatherEffectDefinition>();
            foreach (var weather in weatherDefinitions)
            {
                if (exceptedList == null || exceptedList.Count == 0)
                {
                    newWeatherList.AddRange(weatherDefinitions);
                    break;
                }
                if (exceptedList.Any(x=>x.Equals(weather.Id.SubtypeName, StringComparison.OrdinalIgnoreCase))) continue;
                newWeatherList.Add(weather);
            }
            return newWeatherList[new Random().Next(newWeatherList.Count)];
        }

        public static MyWeatherEffectDefinition GetRandomWeatherFromList(List<string> listedWeather = null, string lastWeather = null)
        {
            if (listedWeather == null || listedWeather.Count == 0) return GetRandomWeather(new List<string>{lastWeather});

            ListReader<MyWeatherEffectDefinition> weatherDefinitions = MyDefinitionManager.Static.GetWeatherDefinitions();
            var newWeatherList = new List<MyWeatherEffectDefinition>();

            foreach (var weatherName in listedWeather)
            {
                if (weatherName.Equals(lastWeather)) continue;
                var def = GetWeatherDefinition(weatherName);
                if (def == null) continue;
                newWeatherList.Add(def);
            }

            return newWeatherList.Count == 0 ? GetWeatherDefinition("Clear") : newWeatherList[new Random().Next(newWeatherList.Count)];
        }

        public static MyWeatherEffectDefinition GetWeatherDefinition(string weatherName)
        {
            MyWeatherEffectDefinition weatherDef = null;
            ListReader<MyWeatherEffectDefinition> weatherDefinitions = MyDefinitionManager.Static.GetWeatherDefinitions();
            foreach (var weather in weatherDefinitions)
            {
                if (!weather.Id.SubtypeName.Equals(weatherName,StringComparison.OrdinalIgnoreCase))continue;
                weatherDef = weather;
                break;
            }

            return weatherDef;
        }

        public static bool TryGetWeather(string weather, out MyWeatherEffectDefinition weatherDef)
        {
            weatherDef = null;
            ListReader<MyWeatherEffectDefinition> weatherDefinitions = MyDefinitionManager.Static.GetWeatherDefinitions();
            foreach (var def in weatherDefinitions)
            {
                if (!def.Id.SubtypeName.Equals(weather,StringComparison.OrdinalIgnoreCase))continue;
                weatherDef = def;
                break;
            }

            return weatherDef != null;

        }

        public static void SetWeatherOnPlanet(MyPlanet planet, MyWeatherEffectDefinition weather)
        {
            float planetRadius = planet.MaximumRadius;
            Vector3D pos = planet.PositionLeftBottomCorner + new Vector3D(planetRadius, planetRadius, planetRadius);
            var currentWeather = string.IsNullOrEmpty(GetWeather(pos))? "Clear" : GetWeather(pos);
            var weatherToSpawn = weather;
            if (weatherToSpawn == null)
            {
                Log.Warn("Can't find weather");
                return;
            }
            var radius = (planetRadius + planetRadius * 0.5f) * 2f;
            SpawnWeather(weatherToSpawn, radius, planet);
            Log.Info($"Changing weather on {planet.Name} from {currentWeather} to {weatherToSpawn.Id.SubtypeName}");

        }

        public static string GetWeather(MyPlanet planet)
        {
            float planetRadius = planet.MaximumRadius;
            Vector3D pos = planet.PositionLeftBottomCorner + new Vector3D(planetRadius, planetRadius, planetRadius);
            var currentWeather = GetWeather(pos);

            return currentWeather;

        }

        [ReflectedGetter(Name = "m_weatherPlanetData", Type = typeof(MySectorWeatherComponent))]
        private static Func<MySectorWeatherComponent, List<MyObjectBuilder_WeatherPlanetData>> _planetWeatherGet;


        public static void SpawnWeather(MyWeatherEffectDefinition weather, float radius, MyPlanet planet)
        {
            var sessionWeather = MySession.Static.GetComponent<MySectorWeatherComponent>();
            var sessionWeatherPlanetData = _planetWeatherGet(sessionWeather);
            if (Math.Abs((double) radius) < 1)
                radius = 0.1122755f * planet.AtmosphereRadius;
            Vector3D translation = MySector.MainCamera.WorldMatrix.Translation;
            var weatherPosition = new Vector3D?(planet.GetClosestSurfacePointGlobal(ref translation));

            MyObjectBuilder_WeatherEffect builderWeatherEffect1 = new MyObjectBuilder_WeatherEffect()
            {
                Weather = weather.Id.SubtypeName,
                Position = weatherPosition.Value,
                Radius = radius
                };
            MyObjectBuilder_WeatherPlanetData weatherPlanetData = new MyObjectBuilder_WeatherPlanetData()
            {
                PlanetId = planet.EntityId
            };
            List<MyObjectBuilder_WeatherEffect> builderWeatherEffectList = new List<MyObjectBuilder_WeatherEffect>();
           
            BoundingSphereD boundingSphereD = new BoundingSphereD(weatherPosition.Value, (double) radius);

            for (int index1 = 0; index1 < sessionWeatherPlanetData.Count; ++index1)
            {
                if (sessionWeatherPlanetData[index1].PlanetId == planet.EntityId)
                {
                    builderWeatherEffectList.Clear();
                    for (int index2 = 0; index2 < sessionWeatherPlanetData[index1].Weathers.Count; ++index2)
                    {
                        BoundingSphereD sphere = new BoundingSphereD(sessionWeatherPlanetData[index1].Weathers[index2].Position, (double) sessionWeatherPlanetData[index1].Weathers[index2].Radius);
                        if (boundingSphereD.Intersects(sphere))
                            builderWeatherEffectList.Add(sessionWeatherPlanetData[index1].Weathers[index2]);
                    }
                    foreach (MyObjectBuilder_WeatherEffect builderWeatherEffect in builderWeatherEffectList)
                        sessionWeatherPlanetData[index1].Weathers.Remove(builderWeatherEffect);
                }
            }

            bool flag = false;

            for (int index = 0; index < sessionWeatherPlanetData.Count; ++index)
            {
                if (sessionWeatherPlanetData[index].PlanetId == planet.EntityId)
                {
                    sessionWeatherPlanetData[index].Weathers.Add(builderWeatherEffect1);
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                weatherPlanetData = new MyObjectBuilder_WeatherPlanetData()
                {
                    PlanetId = planet.EntityId
                };
                weatherPlanetData.Weathers.Add(builderWeatherEffect1);
                sessionWeatherPlanetData.Add(weatherPlanetData);
            }

            SyncWeather();
        }

        public static bool SpawnWeather(MyWeatherEffectDefinition weather, Vector3D position, float radius)
        {
            MyPlanet closestPlanet = MyGamePruningStructure.GetClosestPlanet(position);

            if (closestPlanet == null) return false;

            var sessionWeather = MySession.Static.GetComponent<MySectorWeatherComponent>();
            var sessionWeatherPlanetData = _planetWeatherGet(sessionWeather);
            if (Math.Abs((double) radius) < 1)
                radius = 0.1122755f * closestPlanet.AtmosphereRadius;
            var weatherPosition = new Vector3D?(closestPlanet.GetClosestSurfacePointGlobal(ref position));

            MyObjectBuilder_WeatherEffect builderWeatherEffect1 = new MyObjectBuilder_WeatherEffect()
            {
                Weather = weather.Id.SubtypeName,
                Position = weatherPosition.Value,
                Radius = radius
                };
            List<MyObjectBuilder_WeatherEffect> builderWeatherEffectList = new List<MyObjectBuilder_WeatherEffect>();
           
            BoundingSphereD boundingSphereD = new BoundingSphereD(weatherPosition.Value, (double) radius);

            for (int index1 = 0; index1 < sessionWeatherPlanetData.Count; ++index1)
            {
                if (sessionWeatherPlanetData[index1].PlanetId == closestPlanet.EntityId)
                {
                    builderWeatherEffectList.Clear();
                    for (int index2 = 0; index2 < sessionWeatherPlanetData[index1].Weathers.Count; ++index2)
                    {
                        BoundingSphereD sphere = new BoundingSphereD(sessionWeatherPlanetData[index1].Weathers[index2].Position, (double) sessionWeatherPlanetData[index1].Weathers[index2].Radius);
                        if (boundingSphereD.Intersects(sphere))
                            builderWeatherEffectList.Add(sessionWeatherPlanetData[index1].Weathers[index2]);
                    }
                    foreach (MyObjectBuilder_WeatherEffect builderWeatherEffect in builderWeatherEffectList)
                        sessionWeatherPlanetData[index1].Weathers.Remove(builderWeatherEffect);
                }
            }

            bool flag = false;

            for (int index = 0; index < sessionWeatherPlanetData.Count; ++index)
            {
                if (sessionWeatherPlanetData[index].PlanetId == closestPlanet.EntityId)
                {
                    sessionWeatherPlanetData[index].Weathers.Add(builderWeatherEffect1);
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                var weatherPlanetData = new MyObjectBuilder_WeatherPlanetData()
                {
                    PlanetId = closestPlanet.EntityId
                };
                weatherPlanetData.Weathers.Add(builderWeatherEffect1);
                sessionWeatherPlanetData.Add(weatherPlanetData);
            }

            SyncWeather();

            return true;


        }

        private static MethodInfo _updateWeatherOnClients = typeof(MySectorWeatherComponent).GetMethod("UpdateWeathersOnClients", BindingFlags.NonPublic | BindingFlags.Static);


        private static void SyncWeather()
        {
            var sessionWeather = MySession.Static.GetComponent<MySectorWeatherComponent>();
            var sessionWeatherPlanetData = _planetWeatherGet(sessionWeather);
            NetworkManager.RaiseStaticEvent(_updateWeatherOnClients, sessionWeatherPlanetData.ToArray());
        }

        public static string GetWeather(Vector3D position)
        {
            return MySession.Static.GetComponent<MySectorWeatherComponent>().GetWeather(position);
        }


        public static void LightTheBitchUp(float radius, Vector3D position)
        {
            MySession.Static.GetComponent<MySectorWeatherComponent>().SetWeather("smite", "lightning", radius, position, false);
        }

    }
}