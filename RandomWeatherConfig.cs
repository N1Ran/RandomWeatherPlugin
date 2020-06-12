using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml.Serialization;
using System.Linq;
using Torch;
using Torch.Collections;
using Torch.Views;

namespace RandomWeatherPlugin
{
    [Serializable]
    public class RandomWeatherConfig : ViewModel
    {
        private bool _enable;
        private int _weatherInterval = 60;
        private static RandomWeatherConfig _instance;
        private MtObservableCollection<CustomWeatherRule> _customWeatherRules;

        public static RandomWeatherConfig Instance
        {
            get => _instance ?? (_instance = new RandomWeatherConfig());
            set => _instance = value;
        }

        public RandomWeatherConfig()
        {
            _customWeatherRules = new MtObservableCollection<CustomWeatherRule>();
            _customWeatherRules.CollectionChanged += CustomWeatherRulesOnCollectionChanged;
        }

        private void CustomWeatherRulesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged();
            RandomWeatherPluginCore.Instance.Save();
        }

        [Display(Order = 1, Name = "Enable", Description = "Check to enable the plugin")]
        public bool Enable
        {
            get => _enable;
            set
            {
                _enable = value;
                OnPropertyChanged();
                RandomWeatherPluginCore.Instance.Save();
            }
        }

        [Display(Order = 2, Name = "General Interval", Description = "Main interval used for weather change")]
        public int WeatherInterval
        {
            get => _weatherInterval;
            set
            {
                _weatherInterval = value;
                OnPropertyChanged();
            }
        }

        [Display(Order = 3, EditorType = typeof(EmbeddedCollectionEditor))]
        public MtObservableCollection<CustomWeatherRule> CustomWeatherRules
        {
            get => _customWeatherRules;
            set
            {
                _customWeatherRules = value;
                OnPropertyChanged();
            }
        }

        
        //[XmlIgnore] public MtObservableList<string> ExceptedPlanets { get; } = new MtObservableList<string>();

        /*
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

        */


    }
}