﻿using imBMW.Universal.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Windows.Storage;
using System.ComponentModel;
using Newtonsoft.Json;

namespace imBMW.Universal.App.Tools
{
    public class Settings
    {
        static Settings instance;
        ApplicationDataContainer settings;

        public event PropertyChangedEventHandler PropertyChanged;

        Settings()
        {
            settings = ApplicationData.Current.RoamingSettings;
        }
        
        public List<GaugeSlot> Gauges
        {
            get
            {
                var value = GetValueOrDefault<List<GaugeSlot>>();
                return value ?? DefaultGauges;
            }
            set
            {
                AddOrUpdateValue(value);
            }
        }

        #region Default gauges

        List<GaugeSlot> DefaultGauges
        {
            get
            {
                return new List<GaugeSlot>
                {
                    new GaugeSlot(GaugeType.CoolantTemperature, GaugeType.CoolantTemperature),
                    new GaugeSlot(GaugeType.CoolantRadiatorTemperature, GaugeType.ElectricFanSpeed),
                    new GaugeSlot(GaugeType.AFR),
                    new GaugeSlot(GaugeType.IntakePressure),
                    new GaugeSlot(GaugeType.IntakeTemperature, GaugeType.VoltageBattery),
                    new GaugeSlot(GaugeType.Throttle, GaugeType.AirMass),
                    new GaugeSlot(GaugeType.Consumption1, GaugeType.Consumption2),
                    new GaugeSlot(GaugeType.SpeedLimit, GaugeType.Range)
                };
                /*return new List<GaugeSettings>
                {
                    new GaugeSettings { Name = "Coolant", GaugeType = GaugeType.CoolantTemperature, Format = "N0", Suffix = "°", MinValue = 0, MaxValue = 150, MinYellow = 75, MaxYellow = 95, MaxRed = 105,
                        SecondaryGauge = new GaugeSettings { Name = "Oil", GaugeType = GaugeType.CoolantTemperature, Format = "N0", Suffix = "°", MinValue = 0, MaxValue = 150, MinYellow = 75, MaxYellow = 95, MaxRed = 105}},

                    new GaugeSettings { Name = "Radiator", GaugeType = GaugeType.CoolantRadiatorTemperature, Format = "N0", Suffix = "°", MinValue = 0, MaxValue = 150, MinYellow = 75, MaxYellow = 95, MaxRed = 105,
                        SecondaryGauge = new GaugeSettings { Name = "Fan", GaugeType = GaugeType.ElectricFanSpeed, Suffix = "%", Format = "N0", MaxYellow = 70}},

                    new GaugeSettings { Name = "Lambda", GaugeType = GaugeType.AFR, Format = "F1", Dimension = "Air/Fuel", MinValue = 7.5, MaxValue = 22.5, MinRed = 10, MinYellow = 11, MaxYellow = 14.7, MaxRed = 15.5 },

                    new GaugeSettings { Name = "Boost", GaugeType = GaugeType.IntakePressure, Format = "F2", Dimension = "Bar", MinValue = -1, MaxValue = 1, MinYellow = 0, MaxYellow = 0.5, MaxRed = 0.8, AddToValue = -1000, MultiplyValue = 0.001},

                    new GaugeSettings { Name = "Intake", GaugeType = GaugeType.IntakeTemperature, Format = "N0", Suffix = "°", MinValue = -30, MaxValue = 100, MaxYellow = 30, MaxRed = 60,
                        SecondaryGauge = new GaugeSettings { Name = "Voltage", GaugeType = GaugeType.VoltageBattery, Format = "F1", Suffix = " V", MinValue = 9, MaxValue = 16, MinRed = 13.3, MinYellow = 13.6, MaxYellow = 14.1, MaxRed = 14.5}},

                    new GaugeSettings { Name = "Throttle", GaugeType = GaugeType.Throttle, Format = "N0", Suffix = "%", MinValue = 0, MaxValue = 100, MinYellow = 79,
                        SecondaryGauge = new GaugeSettings { Name = "Air Mass", GaugeType = GaugeType.AirMass, Format = "N0", MinValue = 0, MaxValue = 1500, MinYellow = 500}},

                    new GaugeSettings { Name = "Cons 1", GaugeType = GaugeType.Consumption1, Format = "F1", MinValue = 0, MaxValue = 40,
                        SecondaryGauge = new GaugeSettings { Name = "Cons 2", GaugeType = GaugeType.Consumption2, Format = "F1", MinValue = 0, MaxValue = 40}},

                    new GaugeSettings { Name = "Limit", GaugeType = GaugeType.SpeedLimit, Format = "N0", MinValue = 0, MaxValue = 300, MaxYellow = 80,
                        SecondaryGauge = new GaugeSettings { Name = "Range", GaugeType = GaugeType.Range, Format = "N0", MinValue = 0, MaxValue = 100}},
                };*/
            }
        }

        #endregion

        #region Settings utils

        /// <summary>
        /// Update a setting value for our application. If the setting does not
        /// exist, then add the setting.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool AddOrUpdateValue(Object value, [CallerMemberName]string Key = "")
        {
            bool valueChanged = false;

            // If the key exists
            if (settings.Values.ContainsKey(Key))
            {
                // If the value has changed
                if (settings.Values[Key] != value)
                {
                    // Store the new value
                    settings.Values[Key] = value;
                    valueChanged = true;
                }
            }
            // Otherwise create the key.
            else
            {
                settings.Values.Add(Key, value);
                valueChanged = true;
            }
            if (valueChanged)
            {
                OnPropertyChanged(Key);
            }
            return valueChanged;
        }

        /// <summary>
        /// Get the current value of the setting, or if it is not found, set the 
        /// setting to the default setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        T GetValueOrDefault<T>(Func<T> defaultValueCreator, [CallerMemberName]string Key = "")
        {
            T value;

            // If the key exists, retrieve the value.
            if (settings.Values.ContainsKey(Key))
            {
                value = (T)settings.Values[Key];
            }
            // Otherwise, use the default value.
            else
            {
                if (defaultValueCreator == null)
                {
                    value = default(T);
                }
                else
                {
                    value = defaultValueCreator();
                }
                settings.Values.Add(Key, value);
            }
            return value;
        }

        T GetValueOrDefault<T>([CallerMemberName]string Key = "")
        {
            return GetValueOrDefault<T>(null, Key);
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static Settings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Settings();
                }
                return instance;
            }
        }

        #endregion
    }
}
