using CommandLine;
using Souchy.Net;
using Souchy.Net.io;
using System.IO.Compression;
using System.Xml.Linq;

namespace GodotPackageManager;

internal class Program
{
    private static PackagesFile PackagesFile { get; set; }
    private const string Gpm = ".gpm";
    private const string GpmTemp = ".gpm/temp/";
    private const string GpmPlugins = "packages.json";
    private const string AddonsFolder = "addons";

    static async Task Main(string[] args)
    {
        PackagesFile = Config.Load<PackagesFile>(GpmPlugins);
        PackagesFile.BaseDirectory = Gpm;

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

        PackagesFile.Save();
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
        var asset = PackageFetcher.Instance.FetchPackageDetailsAsync(pkg.AssetId).Result;
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
        string installFolder = Directory.GetDirectories(extractedAddons).First();

        pkg.InstallFolder = installFolder;

        // Move the plugin to addons/
        DirectoryUtil.MoveDirectory(installFolder, AddonsFolder);
        // Remove the zip and extracted directory
        Directory.Delete(extractPath, true);
        File.Delete(zipPath);
    }

    private static async Task AddPackageByName(string name)
    {
        var pkg = (PackageFetcher.Instance.FetchPackagesAsync(name).Result).FirstOrDefault();
        string snake = Naming.ToSnakeCase(pkg.Name);
        if (pkg == null)
        {
            Console.WriteLine("No packages found.");
            return;
        }
        var isNew = PackagesFile.AddPackage(pkg);
        //if (!isNew)
        //    return;

        await InstallPackage(pkg); // FIXME: think this goes out of here? 
    }

    private static async Task RunUninstall(UninstallOptions opts)
    {
        if(!PackagesFile.TryFind(out var pkg, opts.PackageName))
            return;
        //string snake = Naming.ToSnakeCase(pkg.Name);

        Directory.Delete(Path.Combine(AddonsFolder, pkg.InstallFolder), true);
        PackagesFile.Packages.Remove(pkg);
    }

    private static async Task RunUpdate(UpdateOptions opts)
    {
        if (!string.IsNullOrEmpty(opts.PackageName))
        {
            if (!PackagesFile.TryFind(out var pkg, opts.PackageName))
            {
                Console.WriteLine($"Package {opts.PackageName} not found.");
                return;
            }
            await InstallPackage(pkg);
        }
        else
        {
            foreach (var pkg in PackagesFile.Packages)
            {
                await InstallPackage(pkg);
            }
        }
    }

    private static async Task RunList(ListOptions opts)
    {
        if (PackagesFile.Packages.Count == 0)
        {
            Console.WriteLine("No packages installed.");
            return;
        }
        Console.WriteLine("Installed Packages:");
        foreach (var pkg in PackagesFile.Packages)
        {
            Console.WriteLine($"- {pkg.Name} (Version: {pkg.Version})");
        }
    }

}
