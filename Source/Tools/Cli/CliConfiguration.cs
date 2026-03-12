// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Represents the CLI configuration stored at XDG config path.
/// Supports named contexts for connecting to different Chronicle servers.
/// </summary>
public class CliConfiguration
{
    const string DefaultContextName = "default";

    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Gets or sets the name of the currently active context.
    /// </summary>
    public string? ActiveContext { get; set; }

    /// <summary>
    /// Gets or sets the named contexts.
    /// </summary>
    public IDictionary<string, CliContext> Contexts { get; set; } = new Dictionary<string, CliContext>();

    /// <summary>
    /// Gets the name of the active context, falling back to the default name.
    /// </summary>
    [JsonIgnore]
    public string ActiveContextName => ActiveContext ?? DefaultContextName;

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
    /// Automatically migrates legacy flat configuration to the new context-based format.
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
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Detect legacy flat format: has "defaultServer" or "clientId" but no "contexts".
        if (root.TryGetProperty("defaultServer", out _) || root.TryGetProperty("clientId", out _))
        {
            if (!root.TryGetProperty("contexts", out _))
            {
                return MigrateLegacy(root);
            }
        }

        return JsonSerializer.Deserialize<CliConfiguration>(json, _jsonOptions) ?? new CliConfiguration();
    }

    /// <summary>
    /// Gets the active context, creating the default one if none exists.
    /// </summary>
    /// <returns>The current <see cref="CliContext"/>.</returns>
    public CliContext GetCurrentContext()
    {
        var name = ActiveContext ?? DefaultContextName;
        if (Contexts.TryGetValue(name, out var ctx))
        {
            return ctx;
        }

        ctx = new CliContext();
        Contexts[name] = ctx;
        ActiveContext = name;
        return ctx;
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

    /// <summary>
    /// Migrates a legacy flat config to context-based format.
    /// </summary>
    /// <param name="root">The root JSON element with legacy properties.</param>
    /// <returns>A new <see cref="CliConfiguration"/> with the legacy values in a default context.</returns>
    static CliConfiguration MigrateLegacy(JsonElement root)
    {
        var ctx = new CliContext
        {
            Server = GetStringProperty(root, "defaultServer"),
            EventStore = GetStringProperty(root, "defaultEventStore"),
            Namespace = GetStringProperty(root, "defaultNamespace"),
            ClientId = GetStringProperty(root, "clientId"),
            ClientSecret = GetStringProperty(root, "clientSecret"),
            AccessToken = GetStringProperty(root, "accessToken"),
            TokenExpiry = GetStringProperty(root, "tokenExpiry"),
            LoggedInUser = GetStringProperty(root, "loggedInUser")
        };

        var config = new CliConfiguration
        {
            ActiveContext = DefaultContextName,
            Contexts = new Dictionary<string, CliContext> { [DefaultContextName] = ctx }
        };

        // Save the migrated format immediately so we don't re-migrate every load.
        config.Save();
        return config;
    }

    static string? GetStringProperty(JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }

        return null;
    }
}
