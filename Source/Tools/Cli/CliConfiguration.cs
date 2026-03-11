// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Represents the CLI configuration stored at XDG config path.
/// </summary>
public class CliConfiguration
{
    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Gets or sets the default server connection string.
    /// </summary>
    public string? DefaultServer { get; set; }

    /// <summary>
    /// Gets or sets the default event store name.
    /// </summary>
    public string? DefaultEventStore { get; set; }

    /// <summary>
    /// Gets or sets the default namespace name.
    /// </summary>
    public string? DefaultNamespace { get; set; }

    /// <summary>
    /// Gets the path to the configuration file.
    /// </summary>
    /// <returns>The full path to the config file.</returns>
    public static string GetConfigPath()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrWhiteSpace(configHome))
        {
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }

        return Path.Combine(configHome, "cratis", "config.json");
    }

    /// <summary>
    /// Loads the configuration from disk, returning defaults if the file does not exist.
    /// </summary>
    /// <returns>The loaded <see cref="CliConfiguration"/>.</returns>
    public static CliConfiguration Load()
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
        {
            return new CliConfiguration();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<CliConfiguration>(json, _jsonOptions) ?? new CliConfiguration();
    }

    /// <summary>
    /// Saves the configuration to disk, creating the directory if needed.
    /// </summary>
    public void Save()
    {
        var path = GetConfigPath();
        var directory = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(this, _jsonOptions);
        File.WriteAllText(path, json);
    }
}
