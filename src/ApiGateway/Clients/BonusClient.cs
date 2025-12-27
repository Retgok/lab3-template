using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
namespace ApiGatewayService;

public class BonusClient
{
    private readonly HttpClient _client;
    public BonusClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<ApplyBonusResponse?> ApplyAsync(string username, ApplyBonusRequest reqDto)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/privilege/apply");
        req.Headers.Add("X-User-Name", username);
        req.Content = JsonContent.Create(reqDto);
        var resp = await _client.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<ApplyBonusResponse>();
    }

    public async Task<RefundResult> RefundAsync(string username, Guid ticketUid)
    {
        var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/privilege/refund/{ticketUid}"
        );
        req.Headers.Add("X-User-Name", username);

        var resp = await _client.SendAsync(req);

        if (resp.IsSuccessStatusCode)
            return RefundResult.Success;

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return RefundResult.NotNeeded;

        return RefundResult.Retry;
    }

    public async Task<PrivilegeInfoResponse?> GetPrivilegeAsync(string username)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/privilege");
        req.Headers.Add("X-User-Name", username);
        var resp = await _client.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<PrivilegeInfoResponse>();
    }
}
