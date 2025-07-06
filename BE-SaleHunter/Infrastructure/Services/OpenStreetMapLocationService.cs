using System.Text.Json;

namespace BE_SaleHunter.Application.Services
{    public interface ILocationService
    {
        Task<LocationCoordinates?> GeocodeAsync(string address);
        Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude);
        Task<double> CalculateDistanceAsync(decimal lat1, decimal lon1, decimal lat2, decimal lon2);
    }

    public class LocationCoordinates
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

    public class OpenStreetMapLocationService : ILocationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenStreetMapLocationService> _logger;
        private const string NOMINATIM_BASE_URL = "https://nominatim.openstreetmap.org";

        public OpenStreetMapLocationService(HttpClient httpClient, ILogger<OpenStreetMapLocationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            // Set a user agent as required by Nominatim
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SaleHunter/1.0");
        }

        public async Task<LocationCoordinates?> GeocodeAsync(string address)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(address))
                    return null;

                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"{NOMINATIM_BASE_URL}/search?q={encodedAddress}&format=json&limit=1";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<NominatimSearchResult[]>(jsonContent);

                if (results != null && results.Length > 0)
                {
                    var result = results[0];                    if (double.TryParse(result.lat, out var latitude) && 
                        double.TryParse(result.lon, out var longitude))
                    {
                        return new LocationCoordinates
                        {
                            Latitude = (decimal)latitude,
                            Longitude = (decimal)longitude
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding address: {Address}", address);
                return null;
            }
        }

        public async Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude)
        {
            try
            {
                var url = $"{NOMINATIM_BASE_URL}/reverse?lat={latitude}&lon={longitude}&format=json";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<NominatimReverseResult>(jsonContent);

                return result?.display_name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reverse geocoding coordinates: {Latitude}, {Longitude}", latitude, longitude);
                return null;
            }
        }        public Task<double> CalculateDistanceAsync(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            // Haversine formula to calculate distance between two points on Earth
            const double R = 6371; // Earth's radius in kilometers

            var dLat = ToRadians((double)(lat2 - lat1));
            var dLon = ToRadians((double)(lon2 - lon1));

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = R * c;

            return Task.FromResult(distance);
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        // Nominatim response models
        private class NominatimSearchResult
        {
            public string lat { get; set; } = string.Empty;
            public string lon { get; set; } = string.Empty;
            public string display_name { get; set; } = string.Empty;
        }

        private class NominatimReverseResult
        {
            public string display_name { get; set; } = string.Empty;
        }
    }
}
