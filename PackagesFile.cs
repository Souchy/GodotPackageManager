using Souchy.Net.io;

namespace GodotPackageManager;

public class Package
{
    public string AssetId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Godot asset library or NPM.
    /// </summary>
    public string Source { get; set; } = string.Empty;

}

internal class PackagesFile : Config
{
    public List<Package> Packages { get; set; } = [];

    public bool AddPackage(Package package)
    {
        if (TryFind(out var pkg, package.Name))
        {
            if (pkg?.Version == package.Version)
            {
                Console.WriteLine($"Package {package.Name} version {package.Version} is already installed.");
                return false;
            }
            // update existing package
            if (pkg != null) 
                Packages.Remove(pkg);
        }
        Packages.Add(package);
        return true;
    }

    public bool TryFind(out Package? package, string name)
    {
        package = Packages.Find(p => p.Name == name);
        if (package != null)
        {
            return true;
        }
        return false;
    }

}
