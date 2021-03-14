using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;

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

                foreach (FileInfo fileInfo in directoryInfo.GetFiles())
                {
                    if (fileInfo.Extension.ToLower().EndsWith("csv"))
                    {
                        csvFilePath = fileInfo.FullName;
                        continue;
                    }
                    if (fileInfo.Extension.ToLower().EndsWith("srt")) srtFilePath = fileInfo.FullName;
                }
            }

            if (csvFilePath == null || srtFilePath == null) return;

            List<TelemetryData> telemetryDatas = ParseCsv<TelemetryData>(csvFilePath).ToList();
            telemetryDatas.ForEach(t => t.SetCombinedFields());
            RemoveOldValues(telemetryDatas);
            telemetryDatas.AddRange(ParseSrt(srtFilePath, telemetryDatas[0].Date, telemetryDatas[0].Time));
            telemetryDatas = telemetryDatas.OrderBy(t => t.TimeStamp).ToList();
            Prepare(telemetryDatas);
            FileInfo csvFileInfo = new FileInfo(csvFilePath);
            WriteCsv($"{csvFileInfo.DirectoryName}\\result_{csvFileInfo.Name.Replace(csvFileInfo.Extension, "").TrimEnd('.')}.csv", telemetryDatas);
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
                //00:00:00,050 --> 00:00:00,283
                string timestampLine = reader.ReadLine();
                telemetryData.Time = TimeSpan.Parse(timestampLine.Split(' ')[0]).Add(addTimeSpan).ToString();
                //signal:4 ch:1 flightTime:0 uavBat:23.7V glsBat:15.3V uavBatCells:6 glsBatCells:4 delay:27ms bitrate:25.4Mbps rcSignal:0
                string dataLine = reader.ReadLine();
                string[] datas = dataLine.Split(' ');
                telemetryData.VrxBt = double.Parse(datas[4].Split(':')[1].TrimEnd('V'), CultureInfo.InvariantCulture);
                telemetryData.VDelay = int.Parse(datas[7].Split(':')[1].Replace("ms", ""));
                telemetryData.VBitrate = double.Parse(datas[8].Split(':')[1].Replace("Mbps", ""), CultureInfo.InvariantCulture);
                telemetryData.SetCombinedFields();
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
            telemetryDatas.RemoveAt(0);
            telemetryDatas.RemoveAt(telemetryDatas.Count - 1);
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

        private static void Prepare(List<TelemetryData> telemetryDatas)
        {
            TelemetryData lastTelemetryData = telemetryDatas.First();
            int startSeconds = (int)lastTelemetryData.TimeStamp.TimeOfDay.TotalSeconds;
            foreach (TelemetryData telemetryData in telemetryDatas.ToList())
            {
                ReflectionHelper.Merge(lastTelemetryData, telemetryData, PropertyInfos.Values.ToList());

                if (lastTelemetryData.TimeStamp.Second == telemetryData.TimeStamp.Second)
                    telemetryDatas.Remove(telemetryData);
                else telemetryData.LogSecond = (int) telemetryData.TimeStamp.TimeOfDay.TotalSeconds - startSeconds;
                lastTelemetryData = telemetryData;
            }
        }

        private static string PrepareHeaderForMatch(PrepareHeaderForMatchArgs args)
        {
            (string _, PropertyInfo propertyInfo) = PropertyInfos.SingleOrDefault(p => p.Key == args.Header);
            
            return propertyInfo?.Name ?? args.Header;
        }
    }
}
