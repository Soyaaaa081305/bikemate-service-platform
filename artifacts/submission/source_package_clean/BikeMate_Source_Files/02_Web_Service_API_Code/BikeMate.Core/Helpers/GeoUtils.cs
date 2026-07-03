namespace BikeMate.Core.Helpers;

public static class GeoUtils
{
    private const double EarthRadiusKm = 6371d;

    public static decimal DistanceKm(decimal latitudeA, decimal longitudeA, decimal latitudeB, decimal longitudeB)
    {
        var lat1 = ToRadians((double)latitudeA);
        var lat2 = ToRadians((double)latitudeB);
        var deltaLat = ToRadians((double)(latitudeB - latitudeA));
        var deltaLng = ToRadians((double)(longitudeB - longitudeA));
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round((decimal)(EarthRadiusKm * c), 2);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }
}
