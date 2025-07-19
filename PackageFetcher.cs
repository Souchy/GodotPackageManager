using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GodotPackageManager;

internal class PackageFetcher
{
    private const string AssetApiUrl = "https://godotengine.org/asset-library/api/";
    private const string vv = "&godot_version=4.4";
    private const string ss = "&sort=updated";

    //private HttpClient _httpClient;
    private RestClient _httpClient;
    public PackagesFile PackagesFiles { get; set; }

    public static PackageFetcher Instance { get; } = new PackageFetcher();

    private PackageFetcher()
    {
        //var handler = new HttpClientHandler();
        //handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        //_httpClient = new HttpClient(handler);
        //_httpClient.BaseAddress = new Uri(AssetApiUrl);
        //_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GodotPackageManager/1.0");
        //_httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        _httpClient = new RestClient("https://godotengine.org/asset-library/api/");
    }

    public async Task<byte[]> DownloadPackageAsync(GodotAsset asset)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GodotPackageManager/1.0");
            //httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            //var dlurl = Uri.EscapeDataString(asset.download_url);
            var response = await httpClient.GetAsync(asset.download_url);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to download package from {asset.download_url}. Status code: {response.StatusCode}");
            }
            var content = await response.Content.ReadAsByteArrayAsync();
            return content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving or extracting package: {ex.Message} {ex.StackTrace}");
            throw;
        }
    }

    public async Task<List<Package>> FetchPackagesAsync(string packageName)
    {
        var packages = new List<Package>();
        //var url = $"asset?filter={Uri.EscapeDataString(packageName)}" + vv + ss;
        //var response = await _httpClient.GetStringAsync(url);
        var request = new RestRequest("asset", Method.Get);
        request.AddParameter("filter", "SimplestGodRay3D");
        request.AddParameter("godot_version", "4.4");
        request.AddParameter("sort", "updated");
        request.AddHeader("User-Agent", "GodotPackageManager/1.0");
        request.AddHeader("Accept", "application/json");
        try
        {
            Debug.WriteLine($"s: 0");
            Console.WriteLine($"s: 1");
            var response = await _httpClient.ExecuteAsync(request);
            Debug.WriteLine($"status: {response.StatusCode}");
            Console.WriteLine($"status: {response.StatusCode}");

            if (response.IsSuccessful)
            {
                Console.WriteLine(response.Content);
                var assets = JsonSerializer.Deserialize<GodotAssetResponse>(response.Content ?? "");

                if (assets?.result == null)
                    return packages;
                foreach (var asset in assets.result)
                {
                    packages.Add(new Package
                    {
                        AssetId = asset.asset_id,
                        Name = asset.title,
                        Version = asset.version_string,
                        Source = "Godot Asset Library"
                    });
                }
            }
            else
            {
                Console.WriteLine($"Request failed: {response.StatusCode} - {response.StatusDescription}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching packages: {ex.Message}. {ex.StackTrace}");
        }
        return packages;
    }

    public async Task<GodotAsset?> FetchPackageDetailsAsync(string assetId)
    {
        var url = $"asset/{assetId}";
        //var response = await _httpClient.GetStringAsync(url);

        var request = new RestRequest(url, Method.Get);
        request.AddHeader("User-Agent", "GodotPackageManager/1.0");
        request.AddHeader("Accept", "application/json");

        var response = await _httpClient.ExecuteAsync(request);
        return JsonSerializer.Deserialize<GodotAsset>(response.Content ?? "");
        //var asset = JsonSerializer.Deserialize<GodotAsset>(response.Content ?? "");
        //if (asset == null)
        //    throw new Exception($"No package found with ID {assetId}");
        //return new Package
        //{
        //    AssetId = asset.asset_id,
        //    Name = asset.title,
        //    Version = asset.version_string,
        //    Source = "Godot Asset Library"
        //};
    }

}

// Helper classes for JSON deserialization
internal class GodotAssetResponse
{
    public List<GodotAsset> result { get; set; }
}

internal class GodotAsset
{
    public string asset_id { get; set; }
    public string category { get; set; }
    public string category_id { get; set; }
    public string author { get; set; }
    public string author_id { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public string version { get; set; }
    public string version_string { get; set; }
    public string godot_version { get; set; }

    public string cost { get; set; }
    public string support_level { get; set; }

    public string browse_url { get; set; }
    public string issues_url { get; set; }
    public string icon_url { get; set; }
    public string modify_date { get; set; }
    public string download_url { get; set; }
    public string download_commit { get; set; }
    public string download_provider { get; set; }
}
