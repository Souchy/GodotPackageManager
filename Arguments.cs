using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace GodotPackageManager;

[Verb("install", aliases: ["i"], HelpText = "Install packages from the packages.json file or a specific package given")]
public class InstallOptions
{
    [Value(0, HelpText = "Package to install", Required = false)]
    public string PackageName { get; set; } = string.Empty;
    [Value(1, HelpText = "Version of the package to install", Required = false)]
    public string Version { get; set; } = string.Empty;
}

[Verb("uninstall", aliases: ["u"], HelpText = "Uninstall a specific package given")]
public class UninstallOptions
{
    [Value(0, HelpText = "Package to uninstall", Required = true)]
    public string PackageName { get; set; } = string.Empty;
}

[Verb("update", aliases: ["up"], HelpText = "Update packages from the packages.json file or a specific package given to their latest version")]
public class UpdateOptions
{
    [Value(0, HelpText = "Package to update", Required = false)]
    public string PackageName { get; set; } = string.Empty;
}

[Verb("list", aliases: ["l"], HelpText = "List all installed packages")]
public class ListOptions
{
    //[Option('d', "details", HelpText = "Show details of each package")]
    //public bool Details { get; set; } = false;
}