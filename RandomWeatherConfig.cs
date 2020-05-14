using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using Torch;
using Torch.Collections;

namespace RandomWeatherPlugin
{
    public class RandomWeatherConfig:ViewModel
    {
        private bool _enable;
        private int _weatherInterval = 60;


        public bool Enable
        {
            get => _enable;
            set
            {
                _enable = value;
                OnPropertyChanged();
            }
        }

        public int WeatherInterval
        {
            get => _weatherInterval;
            set
            {
                _weatherInterval = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore] public MtObservableList<string> ExceptedPlanets { get; } = new MtObservableList<string>();


        [XmlArray(nameof(ExceptedPlanets))]
        [XmlArrayItem(nameof(ExceptedPlanets), ElementName = "PlanetName")]
        public string[] ExceptedPlanetsSerial
        {
            get => ExceptedPlanets.ToArray();
            set
            {
                ExceptedPlanets.Clear();
                if (value == null) return;
                foreach (var k in value)
                    ExceptedPlanets.Add(k);
            }
        }


        [XmlIgnore] public MtObservableList<string> ExceptedWeathers { get; } = new MtObservableList<string>();

        [XmlArray(nameof(ExceptedWeathers))]
        [XmlArrayItem(nameof(ExceptedWeathers), ElementName = "WeatherSubtypeName")]
        public string[] RemoveBlocksSerial
        {
            get => ExceptedWeathers.ToArray();
            set
            {
                ExceptedWeathers.Clear();
                if (value == null) return;
                foreach (var k in value)
                    ExceptedWeathers.Add(k);
            }
        }

        


    }
}