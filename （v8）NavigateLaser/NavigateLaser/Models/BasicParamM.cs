using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using System.Collections.ObjectModel;

namespace NavigateLaser.Models
{
    class BasicParamM : NotificationObject
    {
        private ObservableCollection<string> resolutions;
        public ObservableCollection<string> Resolutions
        {
            get { return resolutions; }
            set
            {
                resolutions = value;
                this.RaisePropertyChanged("Resolutions");
            }
        }

        private ObservableCollection<string> tableTypes;
        public ObservableCollection<string> TableTypes
        {
            get { return tableTypes; }
            set
            {
                tableTypes = value;
                this.RaisePropertyChanged("TableTypes");
            }
        }

        private string selectedResolution;
        public string SelectedResolution
        {
            get { return selectedResolution; }
            set
            {
                selectedResolution = value;
                this.RaisePropertyChanged("SelectedResolution");
            }
        }

        private string selectedTableType;
        public string SelectedTableType
        {
            get { return selectedTableType; }
            set
            {
                selectedTableType = value;
                this.RaisePropertyChanged("SelectedTableType");
            }
        }

        private ObservableCollection<string> beats;
        public ObservableCollection<string> Beats
        {
            get { return beats; }
            set
            {
                beats = value;
                this.RaisePropertyChanged("Beats");
            }
        }
        private string selectedBeat;
        public string SelectedBeat
        {
            get 
            { 
                return selectedBeat;
            }
            set
            {
                selectedBeat = value;
                this.RaisePropertyChanged("SelectedBeat");
            }
        }

        private string hardwareVersion;
        public string HardwareVersion
        {
            get { return hardwareVersion; }
            set
            {
                hardwareVersion = value;
                this.RaisePropertyChanged("HardwareVersion");
            }
        }

        private string programVersion;
        public string ProgramVersion
        {
            get { return programVersion; }
            set
            {
                programVersion = value;
                this.RaisePropertyChanged("ProgramVersion");
            }
        }

        private string fPGAVersion;
        public string FPGAVersion
        {
            get { return fPGAVersion; }
            set
            {
                fPGAVersion = value;
                this.RaisePropertyChanged("FPGAVersion");
            }
        }

        private string motorState;
        public string MotorState
        {
            get { return motorState; }
            set
            {
                motorState = value;
                this.RaisePropertyChanged("MotorState");
            }
        }

        private string laserState;
        public string LaserState
        {
            get { return laserState; }
            set
            {
                laserState = value;
                this.RaisePropertyChanged("LaserState");
            }
        }

        private string beatState;
        public string BeatState
        {
            get { return beatState; }
            set
            {
                beatState = value;
                this.RaisePropertyChanged("BeatState");
            }
        }

        private string humidity;
        public string Humidity
        {
            get { return humidity; }
            set
            {
                humidity = value;
                this.RaisePropertyChanged("Humidity");
            }
        }

        private string pressure;
        public string Pressure
        {
            get { return pressure; }
            set
            {
                pressure = value;
                this.RaisePropertyChanged("Pressure");
            }
        }

        private string highPressureAPD;
        public string HighPressureAPD
        {
            get { return highPressureAPD; }
            set
            {
                highPressureAPD = value;
                this.RaisePropertyChanged("HighPressureAPD");
            }
        }

        private string temperatureAPD;
        public string TemperatureAPD
        {
            get { return temperatureAPD; }
            set
            {
                temperatureAPD = value;
                this.RaisePropertyChanged("TemperatureAPD");
            }
        }

        private string motorTemperature;
        public string MotorTemperature
        {
            get { return motorTemperature; }
            set
            {
                motorTemperature = value;
                this.RaisePropertyChanged("MotorTemperature");
            }
        }

        private string w5300_SSR;
        public string W5300_SSR
        {
            get { return w5300_SSR; }
            set
            {
                w5300_SSR = value;
                this.RaisePropertyChanged("W5300_SSR");
            }
        }

        private string w5300_IR;
        public string W5300_IR
        {
            get { return w5300_IR; }
            set
            {
                w5300_IR = value;
                this.RaisePropertyChanged("W5300_IR");
            }
        }

        private string w5300_TXFSR;
        public string W5300_TXFSR
        {
            get { return w5300_TXFSR; }
            set
            {
                w5300_TXFSR = value;
                this.RaisePropertyChanged("W5300_TXFSR");
            }
        }

        private string w5300_RXRSR;
        public string W5300_RXRSR
        {
            get { return w5300_RXRSR; }
            set
            {
                w5300_RXRSR = value;
                this.RaisePropertyChanged("W5300_RXRSR");
            }
        }

        private string softReset;
        public string SoftReset
        {
            get { return softReset; }
            set
            {
                softReset = value;
                this.RaisePropertyChanged("SoftReset");
            }
        }

        private string iWDGReset;
        public string IWDGReset
        {
            get { return iWDGReset; }
            set
            {
                iWDGReset = value;
                this.RaisePropertyChanged("IWDGReset");
            }
        }

        private string wWDGReset;
        public string WWDGReset
        {
            get { return wWDGReset; }
            set
            {
                wWDGReset = value;
                this.RaisePropertyChanged("WWDGReset");
            }
        }

        private string pORReset;
        public string PORReset
        {
            get { return pORReset; }
            set
            {
                pORReset = value;
                this.RaisePropertyChanged("PORReset");
            }
        }

        private string nRSTReset;
        public string NRSTReset
        {
            get { return nRSTReset; }
            set
            {
                nRSTReset = value;
                this.RaisePropertyChanged("NRSTReset");
            }
        }

        private string w5500Reset;
        public string W5500Reset
        {
            get { return w5500Reset; }
            set
            {
                w5500Reset = value;
                this.RaisePropertyChanged("W5500Reset");
            }
        }

        private string w5500BeatReset;
        public string W5500BeatReset
        {
            get { return w5500BeatReset; }
            set
            {
                w5500BeatReset = value;
                this.RaisePropertyChanged("W5500BeatReset");
            }
        }

        private string w5500Close;
        public string W5500Close
        {
            get { return w5500Close; }
            set
            {
                w5500Close = value;
                this.RaisePropertyChanged("W5500Close");
            }
        }

        private bool isEnable;
        public bool IsEnable
        {
            get { return isEnable; }
            set
            {
                isEnable = value;
                this.RaisePropertyChanged("IsEnable");
            }
        }

        private double inputVol;
        public double InputVol
        {
            get { return inputVol; }
            set
            {
                inputVol = value;
                this.RaisePropertyChanged("InputVol");
            }
        }

        private string strInputVol;
        public string StrInputVol
        {
            get { return strInputVol; }
            set
            {
                strInputVol = value;
                this.RaisePropertyChanged("StrInputVol");
            }
        }
        private double highVol;
        public double HighVol
        {
            get { return highVol; }
            set
            {
                highVol = value;
                this.RaisePropertyChanged("HighVol");
            }
        }
        private string strHighVol;
        public string StrHighVol
        {
            get { return strHighVol; }
            set
            {
                strHighVol = value;
                this.RaisePropertyChanged("StrHighVol");
            }
        }
        private double apdTemp;
        public double APDTemp
        {
            get { return apdTemp; }
            set
            {
                apdTemp = value;
                this.RaisePropertyChanged("APDTemp");
            }
        }
        private string strAPDTemp;
        public string StrAPDTemp
        {
            get { return strAPDTemp; }
            set
            {
                strAPDTemp = value;
                this.RaisePropertyChanged("StrAPDTemp");
            }
        }
        private double motorTemp;
        public double MotorTemp
        {
            get { return motorTemp; }
            set
            {
                motorTemp = value;
                this.RaisePropertyChanged("MotorTemp");
            }
        }

        private string strMotorTemp;
        public string StrMotorTemp
        {
            get { return strMotorTemp; }
            set
            {
                strMotorTemp = value;
                this.RaisePropertyChanged("StrMotorTemp");
            }
        }
        private double wirelessVol;
        public double WirelessVol
        {
            get { return wirelessVol; }
            set
            {
                wirelessVol = value;
                this.RaisePropertyChanged("WirelessVol");
            }
        }

        private string strWirelessVol;
        public string StrWirelessVol
        {
            get { return strWirelessVol; }
            set
            {
                strWirelessVol = value;
                this.RaisePropertyChanged("StrWirelessVol");
            }
        }
        private string addressOffset;
        public string AddressOffset
        {
            get { return addressOffset; }
            set
            {
                addressOffset = value;
                this.RaisePropertyChanged("AddressOffset");
            }
        }
    }
}
