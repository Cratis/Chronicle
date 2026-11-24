// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

/// <summary>The existing in-process integration host, with test-owned SQLite directories where requested.</summary>
public class NamespaceFenceFixture : ChronicleInProcessFixture
{
    readonly ConcurrentBag<string> _directories = [];

    /// <summary>Creates one isolated directory for a context's SQL files.</summary>
    /// <returns>The test-owned path, cleaned only after host disposal.</returns>
    public string CreateSqliteDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"chronicle-namespace-fence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        _directories.Add(path);
        return path;
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        foreach (var directory in _directories)
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
