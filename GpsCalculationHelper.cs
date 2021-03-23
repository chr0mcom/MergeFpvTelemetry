using System;
using System.Collections.Generic;
using System.Linq;
using GeoCoordinatePortable;

namespace MergeTelemetry
{
    public interface IPosition
    {
        double Latitude { get; set; }
        double Longitude { get; set; }
    }

    public static class GpsCalculationHelper
    {
        private static double ConvertDegreesToRadians(double angle) => Math.PI * angle / 180.0;

        private static double ConvertRadiansToDegrees(double angle) => 180.0 * angle / Math.PI;
        
        public static double CalculateBearing(IPosition position1, IPosition position2)
	    {
		    var lat1 = ConvertDegreesToRadians(position1.Latitude);
		    var lat2 = ConvertDegreesToRadians(position2.Latitude);
		    var long1 = ConvertDegreesToRadians(position2.Longitude);
		    var long2 = ConvertDegreesToRadians(position1.Longitude);
		    var dLon = long1 - long2;

		    var y = Math.Sin(dLon) * Math.Cos(lat2);
		    var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
		    var brng = Math.Atan2(y, x);

		    return (ConvertRadiansToDegrees(brng) + 360) % 360;
	    }
		
        public static double CalculateDistance(IPosition position1, IPosition position2) => new GeoCoordinate(position1.Latitude, position1.Longitude).GetDistanceTo(new GeoCoordinate(position2.Latitude, position2.Longitude));
    }
}
