using CommandLine;
using Souchy.Net.io;
using System.IO.Compression;
using System.Xml.Linq;

namespace GodotPackageManager;

internal class Program
{
    private static PackagesFile PackagesFile { get; set; }
    private const string Gpm = ".gpm";
    private const string GpmTemp = ".gpm/temp/";
    private const string GpmPlugins = ".gpm/packages.json";
    private const string AddonsFolder = "addons";

    static async Task Main(string[] args)
    {
        PackagesFile = Config.Load<PackagesFile>(GpmPlugins);
        if (PackagesFile == null)
        {
            Console.WriteLine("Failed to load packages.json. Please ensure it exists and is valid.");
            Environment.Exit(1);
        }
        PackageFetcher.Instance.PackagesFiles = PackagesFile;

        var command = Parser.Default.ParseArguments<InstallOptions, UninstallOptions, UpdateOptions, ListOptions>(args);
        await command.WithParsedAsync<InstallOptions>(RunInstall);
        await command.WithParsedAsync<UninstallOptions>(RunUninstall);
        await command.WithParsedAsync<UpdateOptions>(RunUpdate);
        await command.WithParsedAsync<ListOptions>(RunList);
        command.WithNotParsed(errs =>
        {
            // Handle errors here if needed
            Console.WriteLine("Error parsing arguments.");
            Environment.Exit(1);
        });
    }

    private static async Task RunInstall(InstallOptions opts)
    {
        if (!string.IsNullOrEmpty(opts.PackageName))
        {
            await AddPackageByName(opts.PackageName);
        }
        else
        {
            foreach (var pkg in PackagesFile.Packages)
            {
                await InstallPackage(pkg);
            }
        }
    }

    private static async Task InstallPackage(Package pkg)
    {
        Console.WriteLine($"Installing package: {pkg.Name} (Version: {pkg.Version})");
        if (string.IsNullOrEmpty(pkg.AssetId))
        {
            await AddPackageByName(pkg.Name);
        }
        var asset = await PackageFetcher.Instance.FetchPackageDetailsAsync(pkg.AssetId);
        if (asset == null)
        {
            Console.WriteLine($"Package with ID {pkg.AssetId} not found.");
            return;
        }

        var content = await PackageFetcher.Instance.DownloadPackageAsync(asset);
        Console.WriteLine($"Package {pkg.AssetId} downloaded successfully.");
        Directory.CreateDirectory(GpmTemp);
        //Directory.CreateDirectory(AddonsFolder);
        string extractPath = GpmTemp + asset.title;

        // Save zip to temp
        var zipPath = $"{GpmTemp}/{asset.title}.zip";
        await File.WriteAllBytesAsync(zipPath, content);
        Console.WriteLine($"Package {asset.title} downloaded successfully.");

        // Extract zip to temp
        ZipFile.ExtractToDirectory(zipPath, extractPath, true);
        Console.WriteLine($"Package {asset.title} unzipped successfully.");

        // Get the plugin's addons directory or the full directory if not found
        string relativeAddons = Path.GetRelativePath("./", AddonsFolder);
        string extractedAddons = Directory.GetDirectories(extractPath, relativeAddons, SearchOption.AllDirectories).FirstOrDefault() ?? extractPath;
        // Move the plugin to addons/
        DirectoryUtil.MoveDirectory(extractedAddons, AddonsFolder);
        // Remove the zip and extracted directory
        Directory.Delete(extractPath, true);
        File.Delete(zipPath);
    }

    private static async Task AddPackageByName(string name)
    {
        var pkg = (await PackageFetcher.Instance.FetchPackagesAsync(name)).FirstOrDefault();
        if (pkg == null)
        {
            Console.WriteLine("No packages found.");
            return;
        }
        var isNew = PackagesFile.AddPackage(pkg);
        //if (!isNew)
        //    return;

        await InstallPackage(pkg); // FIXME: think this goes out of here? 
        PackagesFile.Save();
    }

    private static async Task RunUninstall(UninstallOptions opts)
    {
        throw new NotImplementedException();
    }

    private static async Task RunUpdate(UpdateOptions opts)
    {
        throw new NotImplementedException();
    }

    private static async Task RunList(ListOptions opts)
    {
        throw new NotImplementedException();
    }

}
