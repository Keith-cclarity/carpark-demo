namespace CarparkAvailability.ApiApp.Services;

public interface ISvy21Converter
{
    (double Latitude, double Longitude) ConvertToWgs84(double easting, double northing);
}

public sealed class Svy21Converter : ISvy21Converter
{
    private const double RadRatio = Math.PI / 180d;
    private const double SemiMajorAxis = 6378137d;
    private const double Flattening = 1d / 298.257223563d;
    private const double OriginLatitude = 1.366666d;
    private const double OriginLongitude = 103.833333d;
    private const double FalseNorthing = 38744.572d;
    private const double FalseEasting = 28001.642d;
    private const double ScaleFactor = 1d;

    private const double SemiMinorAxis = SemiMajorAxis * (1d - Flattening);
    private const double EccentricitySquared = (2d * Flattening) - (Flattening * Flattening);
    private const double EccentricityToFourth = EccentricitySquared * EccentricitySquared;
    private const double EccentricityToSixth = EccentricityToFourth * EccentricitySquared;
    private const double N = (SemiMajorAxis - SemiMinorAxis) / (SemiMajorAxis + SemiMinorAxis);
    private const double N2 = N * N;
    private const double N3 = N2 * N;
    private const double N4 = N2 * N2;
    private const double G = SemiMajorAxis * (1d - N) * (1d - N2) * (1d + (9d * N2 / 4d) + (225d * N4 / 64d)) * RadRatio;
    private const double A0 = 1d - (EccentricitySquared / 4d) - (3d * EccentricityToFourth / 64d) - (5d * EccentricityToSixth / 256d);
    private const double A2 = (3d / 8d) * (EccentricitySquared + (EccentricityToFourth / 4d) + (15d * EccentricityToSixth / 128d));
    private const double A4 = (15d / 256d) * (EccentricityToFourth + (3d * EccentricityToSixth / 4d));
    private const double A6 = 35d * EccentricityToSixth / 3072d;

    public (double Latitude, double Longitude) ConvertToWgs84(double easting, double northing)
    {
        double northingPrime = northing - FalseNorthing;
        double meridianDistanceAtOrigin = CalculateMeridianDistance(OriginLatitude);
        double meridianDistancePrime = meridianDistanceAtOrigin + (northingPrime / ScaleFactor);
        double sigma = (meridianDistancePrime / G) * RadRatio;

        double latitudePrime = sigma
            + (((3d * N) / 2d) - ((27d * N3) / 32d)) * Math.Sin(2d * sigma)
            + (((21d * N2) / 16d) - ((55d * N4) / 32d)) * Math.Sin(4d * sigma)
            + ((151d * N3) / 96d) * Math.Sin(6d * sigma)
            + ((1097d * N4) / 512d) * Math.Sin(8d * sigma);

        double sinLatitudePrime = Math.Sin(latitudePrime);
        double sinLatitudePrimeSquared = sinLatitudePrime * sinLatitudePrime;
        double rhoPrime = CalculateRadiusOfCurvatureOfMeridian(sinLatitudePrimeSquared);
        double nuPrime = CalculateRadiusOfCurvatureInPrimeVertical(sinLatitudePrimeSquared);
        double psiPrime = nuPrime / rhoPrime;
        double psiPrimeSquared = psiPrime * psiPrime;
        double psiPrimeCubed = psiPrimeSquared * psiPrime;
        double psiPrimeFourth = psiPrimeCubed * psiPrime;
        double tangentLatitudePrime = Math.Tan(latitudePrime);
        double tangentLatitudePrimeSquared = tangentLatitudePrime * tangentLatitudePrime;
        double tangentLatitudePrimeFourth = tangentLatitudePrimeSquared * tangentLatitudePrimeSquared;
        double tangentLatitudePrimeSixth = tangentLatitudePrimeFourth * tangentLatitudePrimeSquared;
        double eastingPrime = easting - FalseEasting;
        double x = eastingPrime / (ScaleFactor * nuPrime);
        double x2 = x * x;
        double x3 = x2 * x;
        double x5 = x3 * x2;
        double x7 = x5 * x2;

        double latitudeFactor = tangentLatitudePrime / (ScaleFactor * rhoPrime);
        double latitudeTerm1 = latitudeFactor * ((eastingPrime * x) / 2d);
        double latitudeTerm2 = latitudeFactor * ((eastingPrime * x3) / 24d) * ((-4d * psiPrimeSquared) + ((9d * psiPrime) * (1d - tangentLatitudePrimeSquared)) + (12d * tangentLatitudePrimeSquared));
        double latitudeTerm3 = latitudeFactor * ((eastingPrime * x5) / 720d) * (((8d * psiPrimeFourth) * (11d - (24d * tangentLatitudePrimeSquared))) - ((12d * psiPrimeCubed) * (21d - (71d * tangentLatitudePrimeSquared))) + ((15d * psiPrimeSquared) * (15d - (98d * tangentLatitudePrimeSquared) + (15d * tangentLatitudePrimeFourth))) + ((180d * psiPrime) * ((5d * tangentLatitudePrimeSquared) - (3d * tangentLatitudePrimeFourth))) + (360d * tangentLatitudePrimeFourth));
        double latitudeTerm4 = latitudeFactor * ((eastingPrime * x7) / 40320d) * (1385d - (3633d * tangentLatitudePrimeSquared) + (4095d * tangentLatitudePrimeFourth) + (1575d * tangentLatitudePrimeSixth));
        double latitude = latitudePrime - latitudeTerm1 + latitudeTerm2 - latitudeTerm3 + latitudeTerm4;

        double secLatitudePrime = 1d / Math.Cos(latitude);
        double longitudeTerm1 = x * secLatitudePrime;
        double longitudeTerm2 = ((x3 * secLatitudePrime) / 6d) * (psiPrime + (2d * tangentLatitudePrimeSquared));
        double longitudeTerm3 = ((x5 * secLatitudePrime) / 120d) * (((-4d * psiPrimeCubed) * (1d - (6d * tangentLatitudePrimeSquared))) + (psiPrimeSquared * (9d - (68d * tangentLatitudePrimeSquared))) + (72d * psiPrime * tangentLatitudePrimeSquared) + (24d * tangentLatitudePrimeFourth));
        double longitudeTerm4 = ((x7 * secLatitudePrime) / 5040d) * (61d + (662d * tangentLatitudePrimeSquared) + (1320d * tangentLatitudePrimeFourth) + (720d * tangentLatitudePrimeSixth));
        double longitude = (OriginLongitude * RadRatio) + longitudeTerm1 - longitudeTerm2 + longitudeTerm3 - longitudeTerm4;

        return (latitude / RadRatio, longitude / RadRatio);
    }

    private static double CalculateMeridianDistance(double latitudeDegrees)
    {
        double latitudeRadians = latitudeDegrees * RadRatio;
        return SemiMajorAxis * ((A0 * latitudeRadians) - (A2 * Math.Sin(2d * latitudeRadians)) + (A4 * Math.Sin(4d * latitudeRadians)) - (A6 * Math.Sin(6d * latitudeRadians)));
    }

    private static double CalculateRadiusOfCurvatureOfMeridian(double sinSquaredLatitude) =>
        SemiMajorAxis * (1d - EccentricitySquared) / Math.Pow(1d - (EccentricitySquared * sinSquaredLatitude), 1.5d);

    private static double CalculateRadiusOfCurvatureInPrimeVertical(double sinSquaredLatitude) =>
        SemiMajorAxis / Math.Sqrt(1d - (EccentricitySquared * sinSquaredLatitude));
}
