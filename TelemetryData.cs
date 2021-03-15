using System;
using System.Globalization;

namespace MergeTelemetry
{
    public class TelemetryData
    {
        public void ReworkFields()
        {
            if (!string.IsNullOrEmpty(Date) && !string.IsNullOrEmpty(Time))
            {
                TimeStamp = DateTime.Parse(Date);
                TimeSpan timeSpan = TimeSpan.Parse(Time);
                TimeStamp = TimeStamp.AddTicks(timeSpan.Ticks);
            }
            if (!string.IsNullOrEmpty(Gps))
            {
                string[] gps = Gps.Split(' ');
                Latitude = double.Parse(gps[0], CultureInfo.InvariantCulture);
                Longitude = double.Parse(gps[1], CultureInfo.InvariantCulture);
            }
            if (Throttle != 0) Throttle += 2000;
        }

        public int LogSecond { get; set; }

        public DateTime TimeStamp { get; set; }

        [ColumnName("Date")] public string Date { get; set; }

        [ColumnName("Time")] public string Time { get; set; }
        
        [ColumnName("Ptch(rad)")] public double Pitch { get; set; }
        
        [ColumnName("Roll(rad)")] public double Roll { get; set; }
        
        [ColumnName("Yaw(rad)")] public double Yaw { get; set; }
        
        [ColumnName("1RSS(dB)")] public double Rssi1 { get; set; }
        
        [ColumnName("2RSS(dB)")] public double Rssi2 { get; set; }
        
        [ColumnName("TPWR(mW)")] public double TPower { get; set; }
        
        [ColumnName("RQly(%)")] public double RQly { get; set; }
        
        [ColumnName("TQly(%)")] public double TQly { get; set; }
        
        [ColumnName("RxBt(V)")] public double RxBt { get; set; }
        
        [ColumnName("TxBat(V)")] public double TxBt { get; set; }
        
        [ColumnName("Curr(A)")] public double CurrentAmpere { get; set; }
        
        [ColumnName("Capa(mAh)")] public double Capacity { get; set; }
        
        public double Latitude { get; set; }
        
        public double Longitude { get; set; }

        [ColumnName("GPS")] public string Gps { get; set; }
        
        [ColumnName("Alt(m)")] public double Altitude { get; set; }
        
        [ColumnName("GSpd(kmh)")] public double GpsSpeed { get; set; }
        
        [ColumnName("VSpd(m/s)")] public double VSpeed { get; set; }
        
        [ColumnName("Rud")] public double Rudder { get; set; }
        
        [ColumnName("Ele")] public double Elevator { get; set; }
        
        [ColumnName("Thr")] public double Throttle { get; set; }
        
        [ColumnName("Ail")] public double Aileron { get; set; }
        
        public double VrxBt { get; set; }
        
        public int VDelay { get; set; }
        
        public double VBitrate { get; set; }

        public string DataSourceType { get; set; } = "CSV";

        
        public int AverageVDelay { get; set; }
        public double AverageAltitude { get; set; }
        public double AverageGpsSpeed { get; set; }
        public double AverageVSpeed { get; set; }
        public double AverageCurrentAmpere { get; set; }
        public double AverageRxBt { get; set; }
        public double AverageTPower { get; set; }
        public double AverageRssi1 { get; set; }
        public double AverageRssi2 { get; set; }
        public double AverageRQly { get; set; }
        public double AverageTQly { get; set; }
    }
}
