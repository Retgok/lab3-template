using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
namespace ApiGatewayService;

public class TicketsClient
{
    private readonly HttpClient _client;
    public TicketsClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<List<TicketResponse>?> GetAllByUserAsync(string username)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tickets");
        req.Headers.Add("X-User-Name", username);
        var resp = await _client.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        return await resp.Content.ReadFromJsonAsync<List<TicketResponse>>();
    }

    public async Task<TicketResponse?> GetByUidAsync(Guid ticketUid, string username)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/tickets/{ticketUid}");
        req.Headers.Add("X-User-Name", username);
        var resp = await _client.SendAsync(req);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;

        resp.EnsureSuccessStatusCode();

        return await resp.Content.ReadFromJsonAsync<TicketResponse>();
    }

    public async Task<TicketPurchaseResponse?> PurchaseAsync(string username, TicketPurchaseRequest reqDto)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/tickets");
        req.Headers.Add("X-User-Name", username);
        req.Content = JsonContent.Create(reqDto);
        var resp = await _client.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<TicketPurchaseResponse>();
    }

    public async Task<bool> CancelAsync(Guid ticketUid, string username)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/tickets/{ticketUid}");
        req.Headers.Add("X-User-Name", username);
        var resp = await _client.SendAsync(req);
        return resp.IsSuccessStatusCode;
    }
}
