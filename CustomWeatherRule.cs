using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Threading;
using Torch;
using Torch.Views;
using VRage.Collections;

namespace RandomWeatherPlugin
{
    [Serializable]

    public class CustomWeatherRule : ViewModel
    {
        private long _planetId = 0;
        private List<string> _weatherList = new List<string>();
        private int _delay;
        private string _name;


        public override string ToString()
        {
            return PlanetId.ToString();
        }

        [Display(Order = 1, Name = "Planet Ids", Description = "List of planet Id to use with this special rule")]
        public long PlanetId
        {
            get => _planetId;
            set
            {
                _planetId = value;
                OnPropertyChanged();
            }
        }

        [Display(Name = "Weather List", Description = "List of Weather to allow for this special rule")]
        public List<string> WeatherList
        {
            get => _weatherList;
            set
            {
                _weatherList = value;
                OnPropertyChanged();
            }
        }

        [Display(Name = "Interval", Description = "interval for weather change")]
        public int Interval
        {
            get => _delay;
            set
            {
                _delay = value;
                OnPropertyChanged();
            }
        }

    }

    public class MtObservableCollection<T> : ObservableCollection<T>
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var collectionChanged = CollectionChanged;
            if (collectionChanged == null) return;
            foreach (var @delegate in collectionChanged.GetInvocationList())
            {
                var nh = (NotifyCollectionChangedEventHandler) @delegate;
                var dispObj = nh.Target as DispatcherObject;
                var dispatcher = dispObj?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.BeginInvoke((Action)(() => nh.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))), DispatcherPriority.DataBind);
                    continue;
                }
                nh.Invoke(this, e);
            }
        }
    }
}