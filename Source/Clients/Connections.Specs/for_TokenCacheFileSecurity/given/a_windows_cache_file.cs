// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_TokenCacheFileSecurity.given;

public class a_windows_cache_file : Specification
{
    protected string _path;
    protected bool _canRead;
    string _directory;

    void Establish()
    {
        if (OperatingSystem.IsWindows())
        {
            _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "token.json");
            using var stream = TokenCacheFileSecurity.CreateFile(_path);
        }
    }

    void Destroy()
    {
        if (OperatingSystem.IsWindows() && Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
