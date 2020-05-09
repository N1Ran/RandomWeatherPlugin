using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Collections;
using VRage.Game.ModAPI;

namespace RandomWeatherPlugin
{
    [Category("weather")]
    public class Commands : CommandModule
    {
        [Command("set", "Sets specified weather on specified planet")]
        [Permission(MyPromoteLevel.Moderator)]
        public void RequestWeather(string planetName, string weather)
        {
            var foundPlanets = MyEntities.GetEntities().OfType<MyPlanet>();
            var planets = foundPlanets.Where(x => x.Name.Contains(planetName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (planets.Count == 0)
            {
                Context.Respond("Can't find that planet on the server");
                return;
            }

            if (weather.Equals("random", StringComparison.OrdinalIgnoreCase))
                weather = WeatherGenerator.GetRandomWeather().Id.SubtypeName;

            if (!WeatherGenerator.TryGetWeather(weather, out var weatherDef))
            {
                Context.Respond("Weather type not found");
                return;
            }

            var count = 0;
            foreach (var planet in planets)
            {
                WeatherGenerator.SetWeatherOnPlanet(planet,weatherDef);
                count++;
            }
            Context.Respond($"Setting {weather} for {count} planets with the name {planetName}");
        }

        [Command("clear all", "Sets all planets' weather to clear")]
        public void ClearAll()
        {
            var planets = MyEntities.GetEntities().OfType<MyPlanet>().ToList();
            ListReader<MyWeatherEffectDefinition> weatherDefinitions = MyDefinitionManager.Static.GetWeatherDefinitions();
            var weatherDef = weatherDefinitions.FirstOrDefault(x => x.Id.SubtypeName.Equals("Clear"));
            if (planets.Count == 0)
            {
                Context.Respond("No Planet Found");
                return;
            }

            var count = 0;
            foreach (var planet in planets)
            {
                WeatherGenerator.SetWeatherOnPlanet(planet,weatherDef);
                count++;
            }
            Context.Respond($"Clearing up the weather on {count} planets");

        }

        [Command("clear", "Sets specified planet's weather to clear")]
        public void Clear(string planetName)
        {
            var planets = MyEntities.GetEntities().OfType<MyPlanet>().ToList();
            ListReader<MyWeatherEffectDefinition> weatherDefinitions = MyDefinitionManager.Static.GetWeatherDefinitions();
            var weatherDef = weatherDefinitions.FirstOrDefault(x => x.Id.SubtypeName.Equals("Clear"));
            if (planets.Count == 0)
            {
                Context.Respond("No Planet Found");
                return;
            }

            var count = 0;
            foreach (var planet in planets)
            {
                if (planet.Name != planetName) continue;
                count++;
                WeatherGenerator.SetWeatherOnPlanet(planet,weatherDef);
            }

            Context.Respond($"Clearing up the weather on {count} planets");

        }

        [Command("list","Lists all possible weather")]
        public void ListWeather()
        {
            ListReader<MyWeatherEffectDefinition> weatherDefinitions = MyDefinitionManager.Static.GetWeatherDefinitions();
            if (weatherDefinitions.Count == 0)
            {
                Context.Respond("No weather definition found");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Found {weatherDefinitions.Count} weather definitions");
            foreach (var weatherDef in weatherDefinitions)
            {
                sb.AppendLine($"{weatherDef.Id.SubtypeName}");
            }

            Context.Respond(sb.ToString());

        }

        [Command("get", "gets current weather on planets with specified name")]
        public void GetWeather(string planetName = null)
        {
            var foundPlanets = MyEntities.GetEntities().OfType<MyPlanet>();

            var planets = string.IsNullOrEmpty(planetName)? foundPlanets.ToList():foundPlanets.Where(x => x.Name.Contains(planetName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (planets.Count == 0)
            {
                Context.Respond("Can't find requested planet on the server");
                return;
            }
            var sb = new StringBuilder();
            sb.AppendLine("PlanetName: WeatherSubtypeId");
            foreach (var planet in planets)
            {
                var weather = string.IsNullOrEmpty(WeatherGenerator.GetWeather(planet))? "None Set" : WeatherGenerator.GetWeather(planet);
                sb.AppendLine($"{planet.Name}: {weather}");
            }

            Context.Respond(sb.ToString());
        }


    }
}
