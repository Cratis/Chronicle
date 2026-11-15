// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Writes non-sensitive, structured measurement data.</summary>
internal static class ResultFiles
{
    static readonly JsonSerializerOptions _options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Writes an indented JSON result.</summary>
    /// <typeparam name="T">The result shape.</typeparam>
    /// <param name="path">The result path.</param>
    /// <param name="value">Non-sensitive data only.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The write task.</returns>
    public static async Task Write<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, _options), cancellationToken);
    }
}
