using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Collections;
using VRage.Game.ModAPI;

namespace RandomWeatherPlugin
{
    [Category("weather")]
    public class Commands : CommandModule
    {
        [Command("set")]
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

        [Command("clear all")]
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

        [Command("clear")]
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
    }
}
