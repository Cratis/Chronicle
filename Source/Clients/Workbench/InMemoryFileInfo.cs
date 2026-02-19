// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Cratis.Chronicle.Workbench.Embedded;

/// <summary>
/// Represents an in-memory file.
/// </summary>
/// <param name="path">The path of the file.</param>
/// <param name="content">The content of the file.</param>
public class InMemoryFileInfo(string path, string content) : IFileInfo
{
    /// <inheritdoc/>
    public bool Exists => true;

    /// <inheritdoc/>
    public long Length => content.Length;

    /// <inheritdoc/>
    public string? PhysicalPath => null;

    /// <inheritdoc/>
    public string Name => path;

    /// <inheritdoc/>
    public DateTimeOffset LastModified => DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    public bool IsDirectory => false;

    /// <inheritdoc/>
    public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(content));
}
