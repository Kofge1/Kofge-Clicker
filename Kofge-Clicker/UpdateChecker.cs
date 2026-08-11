using System.Net.Http.Headers;
using System.Text.Json;

namespace KofgeClicker;

internal sealed record UpdateInfo(
    string TagName,
    string ReleaseUrl,
    string DownloadUrl,
    long? AssetSize,
    string? Sha256Digest);

internal static class UpdateChecker
{
    internal const string ReleaseAssetName = "Kofge-Clicker.exe";
    private const string LatestReleaseApiUrl = "https://api.github.com/repos/Kofge1/Kofge-Clicker/releases/latest";
    private const string LatestReleaseUrl = "https://github.com/Kofge1/Kofge-Clicker/releases/latest";
    private const string ReleasesBaseUrl = "https://github.com/Kofge1/Kofge-Clicker/releases";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    internal static async Task<UpdateInfo?> CheckForUpdateAsync(string currentVersion, CancellationToken cancellationToken)
    {
        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = true };
            using var client = new HttpClient(handler) { Timeout = Timeout };
            ConfigureClient(client, currentVersion);

            var latest = await TryGetLatestFromApiAsync(client, cancellationToken).ConfigureAwait(false)
                ?? await TryGetLatestFromRedirectAsync(client, cancellationToken).ConfigureAwait(false);

            return latest is not null && IsNewerVersion(latest.TagName, currentVersion)
                ? latest
                : null;
        }
        catch
        {
            return null;
        }
    }

    internal static bool IsNewerVersion(string candidateVersion, string currentVersion)
    {
        return TryParseComparableVersion(candidateVersion, out var candidate)
            && TryParseComparableVersion(currentVersion, out var current)
            && candidate > current;
    }

    internal static bool VersionsEqual(string firstVersion, string secondVersion)
    {
        return TryParseComparableVersion(firstVersion, out var first)
            && TryParseComparableVersion(secondVersion, out var second)
            && first == second;
    }

    internal static string NormalizeVersion(string version)
    {
        var normalized = version.Trim();
        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[1..];
        }

        return normalized;
    }

    private static void ConfigureClient(HttpClient client, string currentVersion)
    {
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("Kofge-Clicker", NormalizeVersion(currentVersion)));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
    }

    private static async Task<UpdateInfo?> TryGetLatestFromApiAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(LatestReleaseApiUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = document.RootElement;
            var latestTag = GetString(root, "tag_name");
            if (string.IsNullOrWhiteSpace(latestTag))
            {
                return null;
            }

            var releaseUrl = GetString(root, "html_url");
            if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var asset in assets.EnumerateArray())
            {
                if (!string.Equals(GetString(asset, "name"), ReleaseAssetName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var downloadUrl = GetString(asset, "browser_download_url");
                if (!IsTrustedDownloadUrl(downloadUrl))
                {
                    return null;
                }

                long? size = asset.TryGetProperty("size", out var sizeElement) && sizeElement.TryGetInt64(out var parsedSize)
                    ? parsedSize
                    : null;
                return new UpdateInfo(
                    latestTag.Trim(),
                    string.IsNullOrWhiteSpace(releaseUrl) ? LatestReleaseUrl : releaseUrl.Trim(),
                    downloadUrl!.Trim(),
                    size,
                    NormalizeDigest(GetString(asset, "digest")));
            }

            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<UpdateInfo?> TryGetLatestFromRedirectAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(
                LatestReleaseUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var finalUri = response.RequestMessage?.RequestUri;
            var tag = TryExtractTag(finalUri);
            if (string.IsNullOrWhiteSpace(tag))
            {
                return null;
            }

            var escapedTag = Uri.EscapeDataString(tag);
            return new UpdateInfo(
                tag,
                $"{ReleasesBaseUrl}/tag/{escapedTag}",
                $"{ReleasesBaseUrl}/download/{escapedTag}/{ReleaseAssetName}",
                null,
                null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryExtractTag(Uri? uri)
    {
        if (uri is null || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        const string marker = "/Kofge1/Kofge-Clicker/releases/tag/";
        var path = uri.AbsolutePath;
        var markerIndex = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var tag = Uri.UnescapeDataString(path[(markerIndex + marker.Length)..]).Trim('/');
        return tag.Length > 0 ? tag : null;
    }

    private static bool TryParseComparableVersion(string version, out Version comparable)
    {
        comparable = new Version(0, 0, 0, 0);
        if (!Version.TryParse(NormalizeVersion(version), out var parsed))
        {
            return false;
        }

        comparable = new Version(
            parsed.Major,
            parsed.Minor,
            Math.Max(0, parsed.Build),
            Math.Max(0, parsed.Revision));
        return true;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static bool IsTrustedDownloadUrl(string? downloadUrl)
    {
        return Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeDigest(string? digest)
    {
        const string prefix = "sha256:";
        if (string.IsNullOrWhiteSpace(digest) || !digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var value = digest[prefix.Length..].Trim();
        return value.Length == 64 && value.All(Uri.IsHexDigit)
            ? value.ToUpperInvariant()
            : null;
    }
}
