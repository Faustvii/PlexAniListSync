using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PlexAniListSync.AniListNet.Helpers;
using PlexAniListSync.AniListNet.Objects;

namespace PlexAniListSync.AniListNet;

public partial class AniClient(HttpClient client)
{
    private const int RawBodySnippetLength = 512;
    private readonly Uri _url = new("https://graphql.anilist.co");

    public bool IsAuthenticated { get; private set; }

    /// <summary>
    /// The authenticated user. Null when <see cref="IsAuthenticated"/> is false. When true, may still be null
    /// if not loaded with <see cref="TryAuthenticateAsync"/>.
    /// </summary>
    public User? AuthenticatedUser { get; private set; }

    public event EventHandler<AniRateEventArgs>? RateChanged;

    public AniClient() : this(new HttpClient())
    {
    }

    public void SetAuthenticationHeader(string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        IsAuthenticated = true;
    }

    /// <summary>
    /// Sets the authentication header, and loads <see cref="AuthenticatedUser"/> if successful.
    /// Exceptions other than <see cref="HttpStatusCode.Unauthorized"/> are rethrown.
    /// </summary>
    public async Task<bool> TryAuthenticateAsync(string token)
    {
        SetAuthenticationHeader(token);
        try
        {
            AuthenticatedUser = await GetAuthenticatedUserAsync();
            IsAuthenticated = true;
        }
        catch (AniException aniException)
        {
            if (aniException.StatusCode != HttpStatusCode.Unauthorized)
                throw;

            client.DefaultRequestHeaders.Authorization = null;
            IsAuthenticated = false;
            AuthenticatedUser = null;
        }

        return IsAuthenticated;
    }

    private async Task<JsonNode> PostRequestAsync(GqlSelection selection, bool isMutation = false,
        CancellationToken cancellationToken = default)
    {
        var query = (isMutation ? "mutation" : string.Empty) + selection;
        var requestPayload = JsonSerializer.Serialize(new { query });
        var body = new StringContent(requestPayload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(_url, body, cancellationToken);

        var retryAfter = GetHeaderInt("Retry-After");
        var rateLimit = GetHeaderInt("X-RateLimit-Limit");
        var rateRemaining = GetHeaderInt("X-RateLimit-Remaining");
        var rateReset = GetHeaderInt("X-RateLimit-Reset");

        if (rateLimit.HasValue && rateRemaining.HasValue)
        {
            RateChanged?.Invoke(this, new AniRateEventArgs(
                rateLimit.Value,
                rateRemaining.Value,
                retryAfter,
                rateReset
            ));
        }

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new AniListRateLimitException(query, responseText, retryAfter, rateReset);

        var responseJson = TryParseJson(responseText);

        if (!response.IsSuccessStatusCode)
        {
            var message = TryGetGraphQlErrorMessage(responseJson)
                          ?? $"AniList request failed with status {(int)response.StatusCode} "
                             + $"({response.StatusCode}): {Truncate(responseText)}";
            throw new AniException(message, query, responseText, response.StatusCode);
        }

        if (responseJson is null)
            throw new AniException(
                $"Expected a JSON response but received: {Truncate(responseText)}",
                query, responseText, response.StatusCode);

        var graphQlError = TryGetGraphQlErrorMessage(responseJson);
        if (graphQlError is not null)
            throw new AniException(graphQlError, query, responseText, response.StatusCode);

        return responseJson["data"]
               ?? throw new AniException("AniList response contained no data.", query, responseText,
                   response.StatusCode);

        int? GetHeaderInt(string headerName)
        {
            return response.Headers.TryGetValues(headerName, out var values)
                   && int.TryParse(values.FirstOrDefault(), out var val)
                ? val
                : null;
        }
    }

    private static JsonNode? TryParseJson(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return null;
        try
        {
            return JsonNode.Parse(responseText);
        }
        catch (JsonException)
        {
            // A non-JSON body (e.g. an HTML error page) is reported via a raw-body snippet instead of crashing.
            return null;
        }
    }

    private static string? TryGetGraphQlErrorMessage(JsonNode? responseJson)
    {
        var errors = responseJson?["errors"];
        if (errors is null)
            return null;
        var firstMessage = errors is JsonArray { Count: > 0 } array ? array[0]?["message"]?.GetValue<string>() : null;
        return firstMessage ?? "Unknown GraphQL error";
    }

    private static string Truncate(string value)
    {
        value = value.Trim();
        return value.Length <= RawBodySnippetLength ? value : value[..RawBodySnippetLength] + "...";
    }
}
