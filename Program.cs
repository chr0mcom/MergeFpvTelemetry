using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using MathNet.Numerics.Statistics;

namespace MergeTelemetry
{
    class Program
    {
        private static readonly Dictionary<string, PropertyInfo> PropertyInfos = new Dictionary<string, PropertyInfo>();
        static void Main(string[] args)
        {
            foreach (PropertyInfo propertyInfo in typeof(TelemetryData).GetProperties())
            {
                ColumnNameAttribute attribute = propertyInfo.GetCustomAttributes().ToList().SingleOrDefault() as ColumnNameAttribute;

                PropertyInfos.Add(attribute?.ColumnName ?? propertyInfo.Name, propertyInfo);
            }
            
            string csvFilePath = null;
            string srtFilePath = null;
            if (args?.Length > 1)
            {
                csvFilePath = args[0];
                srtFilePath = args[1];
            }
            else
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(Environment.CurrentDirectory);

                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    if (file.Extension.ToLower().EndsWith("csv"))
                    {
                        csvFilePath = file.FullName;
                        continue;
                    }
                    if (file.Extension.ToLower().EndsWith("srt")) srtFilePath = file.FullName;
                }
            }

            if (csvFilePath == null && srtFilePath == null) return;

            List<TelemetryData> telemetryDatas = new List<TelemetryData>();
            if (csvFilePath != null) telemetryDatas.AddRange(ParseCsv<TelemetryData>(csvFilePath));
            else telemetryDatas.Add(new TelemetryData {TimeStamp = File.GetCreationTime(srtFilePath)});
            telemetryDatas.ForEach(t => t.ReworkFields());
            RemoveOldValues(telemetryDatas);

            if (srtFilePath != null) telemetryDatas.AddRange(ParseSrt(srtFilePath, telemetryDatas[0].TimeStamp.ToShortDateString(), telemetryDatas[0].TimeStamp.ToString("HH:mm:ss.ffff")));

            telemetryDatas = Prepare(telemetryDatas.OrderBy(t => t.TimeStamp));
            FileInfo fileInfo = new FileInfo(csvFilePath ?? srtFilePath);
            WriteCsv($"{fileInfo.DirectoryName}\\result_{fileInfo.Name.Replace(fileInfo.Extension, "").TrimEnd('.')}.csv", telemetryDatas);
        }

        private static void WriteCsv(string filePath, List<TelemetryData> telemetryDatas)
        {
            using StreamWriter writer = new StreamWriter(filePath, false);
            using CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(telemetryDatas);
        }

        private static List<TelemetryData> ParseSrt(string filePath, string date, string time)
        {
            List<TelemetryData> telemetryDatas = new List<TelemetryData>();
            using TextReader reader = File.OpenText(filePath);
            TimeSpan addTimeSpan = TimeSpan.Parse(time);
            string line = string.Empty;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line)) continue;
                TelemetryData telemetryData = new TelemetryData {Date = date, DataSourceType = "Srt"};
                string timestampLine = reader.ReadLine();
                telemetryData.Time = TimeSpan.Parse(timestampLine.Split(' ')[0]).Add(addTimeSpan).ToString();
                string dataLine = reader.ReadLine();
                string[] datas = dataLine.Split(' ');
                telemetryData.VrxBt = double.Parse(datas[4].Split(':')[1].TrimEnd('V'), CultureInfo.InvariantCulture);
                telemetryData.VDelay = int.Parse(datas[7].Split(':')[1].Replace("ms", ""));
                telemetryData.VBitrate = double.Parse(datas[8].Split(':')[1].Replace("Mbps", ""), CultureInfo.InvariantCulture);
                telemetryData.ReworkFields();
                telemetryDatas.Add(telemetryData);
            }

            return telemetryDatas;
        }

        private static IEnumerable<T> ParseCsv<T>(string filePath)
        {
            using TextReader reader = File.OpenText(filePath);
            CsvConfiguration csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
                                                {
                                                        PrepareHeaderForMatch = PrepareHeaderForMatch, Delimiter = ",", MissingFieldFound = null, BadDataFound = null, HeaderValidated = null
                                                };
            CsvReader csv = new CsvReader(reader, csvConfiguration);
            while (csv.Read()) yield return csv.GetRecord<T>();
        }

        private static void RemoveOldValues(List<TelemetryData> telemetryDatas)
        {
            if (telemetryDatas.Count > 10)
            {
                telemetryDatas.RemoveAt(0);
                telemetryDatas.RemoveAt(telemetryDatas.Count - 1);
            }
            TelemetryData lastTelemetryData = telemetryDatas.First();
            TelemetryData interuptedTelemetryData = null;
            foreach (TelemetryData telemetryData in telemetryDatas)
            {
                if (telemetryData.TimeStamp.TimeOfDay.TotalSeconds - lastTelemetryData.TimeStamp.TimeOfDay.TotalSeconds > 10)
                {
                    interuptedTelemetryData = telemetryData;
                    break;
                }

                lastTelemetryData = telemetryData;
            }

            if (interuptedTelemetryData == null) return;
            int indexOfInteruptedTelemetryData = telemetryDatas.IndexOf(interuptedTelemetryData);
            telemetryDatas.RemoveRange(0, indexOfInteruptedTelemetryData);
        }

        private static List<TelemetryData> Prepare(IEnumerable<TelemetryData> telemetryDatas)
        {
            List<TelemetryData> retTelemetryDatas = new List<TelemetryData>();
            TelemetryData lastTelemetryData = telemetryDatas.First();
            int startSeconds = (int)lastTelemetryData.TimeStamp.TimeOfDay.TotalSeconds;
            foreach (TelemetryData telemetryData in telemetryDatas)
            {
                ReflectionHelper.Merge(lastTelemetryData, telemetryData, PropertyInfos.Values.ToList());
                if (lastTelemetryData.TimeStamp.Second != telemetryData.TimeStamp.Second)
                {
                    telemetryData.LogSecond = (int) telemetryData.TimeStamp.TimeOfDay.TotalSeconds - startSeconds;
                    retTelemetryDatas.Add(telemetryData);

                    telemetryData.AverageVDelay = (int)retTelemetryDatas.Select(t => (double) t.VDelay).Mean();
                    telemetryData.AverageAltitude = retTelemetryDatas.Select(t => t.Altitude).Mean();
                    telemetryData.AverageGpsSpeed = retTelemetryDatas.Select(t => t.GpsSpeed).Mean();
                    telemetryData.AverageVSpeed = retTelemetryDatas.Select(t => t.VSpeed).Mean();
                    telemetryData.AverageCurrentAmpere = retTelemetryDatas.Select(t => t.CurrentAmpere).Mean();
                    telemetryData.AverageRxBt = retTelemetryDatas.Select(t => t.RxBt).Mean();
                    telemetryData.AverageTPower = retTelemetryDatas.Select(t => t.TPower).Mean();
                    telemetryData.AverageRssi1 = retTelemetryDatas.Select(t => t.Rssi1).Mean();
                    telemetryData.AverageRssi2 = retTelemetryDatas.Select(t => t.Rssi2).Mean();
                    telemetryData.AverageRQly = retTelemetryDatas.Select(t => t.RQly).Mean();
                    telemetryData.AverageTQly = retTelemetryDatas.Select(t => t.TQly).Mean();
                }
                lastTelemetryData = telemetryData;
            }

            return retTelemetryDatas;
        }

        private static string PrepareHeaderForMatch(PrepareHeaderForMatchArgs args)
        {
            (string _, PropertyInfo propertyInfo) = PropertyInfos.SingleOrDefault(p => p.Key == args.Header);
            
            return propertyInfo?.Name ?? args.Header;
        }
    }
}
