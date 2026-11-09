// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Resolves the connection settings that name a file holding their value, such as a Kubernetes Secret mounted into the pod.
/// </summary>
/// <remarks>
/// <para>
/// Every connection-bearing setting accepts a sibling ending in <c language="text">File</c>:
/// <c language="text">storage.connectionDetailsFile</c>, <c language="text">sinks.configurations.&lt;name&gt;.connectionDetailsFile</c>
/// and <c language="text">sinks.configurations.&lt;name&gt;.passwordFile</c> for a Neo4j sink.
/// </para>
/// <para>
/// Each file is read once, at startup, and trimmed of a trailing newline. Its value is written into the configuration in place
/// of the setting it stands for, so everything that reads the setting gets it without knowing where it came from. A password is
/// written beside the sink and <see cref="SinkConfiguration.PasswordSecret"/> is pointed at it, so it is never part of the options.
/// </para>
/// </remarks>
public static class ConnectionFiles
{
    /// <summary>
    /// The configuration key, under a sink configuration, that holds a password read from <see cref="SinkConfiguration.PasswordFile"/>.
    /// </summary>
    public const string PasswordFromFileKey = "PasswordFromFile";

    const string FileSuffix = "File";

    /// <summary>
    /// Resolve every connection setting under <see cref="ChronicleOptions.SectionPath"/> that names a file.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to resolve in, and write the values to.</param>
    /// <exception cref="InvalidConnectionFile">Thrown when a setting and its file sibling are both set, or a file cannot be read.</exception>
    public static void Resolve(IConfiguration configuration)
    {
        var chronicle = configuration.GetSection(ChronicleOptions.SectionPath);

        ResolveValue(chronicle.GetSection(nameof(ChronicleOptions.Storage)), "storage", nameof(Storage.ConnectionDetails));

        var sinks = chronicle.GetSection($"{nameof(ChronicleOptions.Sinks)}:{nameof(SinksOptions.Configurations)}");
        foreach (var sink in sinks.GetChildren())
        {
            var prefix = $"sinks.configurations.{sink.Key}";
            ResolveValue(sink, prefix, nameof(SinkConfiguration.ConnectionDetails));
            ResolvePassword(sink, prefix);
        }
    }

    static void ResolveValue(IConfigurationSection section, string prefix, string setting)
    {
        var fileSetting = $"{setting}{FileSuffix}";
        var path = section[fileSetting];
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var qualifiedFileSetting = Qualify(prefix, fileSetting);
        if (!string.IsNullOrEmpty(section[setting]))
        {
            throw BothAreSet(qualifiedFileSetting, Qualify(prefix, setting));
        }

        section[setting] = Read(qualifiedFileSetting, path);
    }

    static void ResolvePassword(IConfigurationSection sink, string prefix)
    {
        var path = sink[nameof(SinkConfiguration.PasswordFile)];
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var qualifiedFileSetting = Qualify(prefix, nameof(SinkConfiguration.PasswordFile));
        if (!string.IsNullOrEmpty(sink[nameof(SinkConfiguration.PasswordSecret)]))
        {
            throw BothAreSet(qualifiedFileSetting, Qualify(prefix, nameof(SinkConfiguration.PasswordSecret)));
        }

        sink[PasswordFromFileKey] = Read(qualifiedFileSetting, path);
        sink[nameof(SinkConfiguration.PasswordSecret)] = ConfigurationPath.Combine(sink.Path, PasswordFromFileKey);
    }

    static string Read(string setting, string path)
    {
        if (Directory.Exists(path))
        {
            throw new InvalidConnectionFile(setting, $"'{path}' is a directory, not a file.");
        }

        if (!File.Exists(path))
        {
            throw new InvalidConnectionFile(setting, $"the file '{path}' does not exist.");
        }

        string content;
        try
        {
            content = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidConnectionFile(setting, $"the file '{path}' could not be read.");
        }

        var value = content.TrimEnd('\r', '\n');
        if (value.Length == 0)
        {
            throw new InvalidConnectionFile(setting, $"the file '{path}' is empty.");
        }

        return value;
    }

    static InvalidConnectionFile BothAreSet(string fileSetting, string setting) =>
        new(fileSetting, $"'{setting}' is also set. Set either '{setting}' or '{fileSetting}', not both.");

    static string Qualify(string prefix, string setting) => $"{prefix}.{char.ToLowerInvariant(setting[0])}{setting[1..]}";
}
