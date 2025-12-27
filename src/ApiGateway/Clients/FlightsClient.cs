using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
namespace ApiGatewayService;

public class FlightsClient
{
    private readonly HttpClient _client;
    public FlightsClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<FlightResponse?> GetByFlightNumberAsync(string flightNumber, string username)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/flights/{Uri.EscapeDataString(flightNumber)}");
        req.Headers.Add("X-User-Name", username);
        var resp = await _client.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<FlightResponse>();
    }

    public async Task<PaginationResponse?> GetAllAsync(int page = 0, int size = 10)
    {
        var resp = await _client.GetAsync($"/api/v1/flights?page={page}&size={size}");
        if (!resp.IsSuccessStatusCode) return null;

        var flights = await resp.Content.ReadFromJsonAsync<List<FlightResponse>>();
        if (flights == null) return null;

        return new PaginationResponse
        {
            Page = page,
            PageSize = size,
            TotalElements = flights.Count,
            Items = flights
        };
    }

}
