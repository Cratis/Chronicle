// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.Workbench.Embedded;

/// <summary>
/// Represents a file provider that serves static files.
/// </summary>
/// <param name="innerFileProvider">The actual provider of the files.</param>
/// <param name="basePath">The base path to use.</param>
/// <param name="indexFile">The content to replace the index file with.</param>
public class StaticFilesFileProvider(IFileProvider innerFileProvider, string basePath, string indexFile) : IFileProvider
{
    /// <inheritdoc/>
    public IDirectoryContents GetDirectoryContents(string subpath) => innerFileProvider.GetDirectoryContents(subpath);

    /// <inheritdoc/>
    public IFileInfo GetFileInfo(string subpath)
    {
        if (subpath == "/index.html")
        {
            return new InMemoryFileInfo(subpath, indexFile);
        }

        if (!subpath.StartsWith('/'))
        {
            subpath = $"/{subpath}";
        }

        if (subpath.StartsWith(basePath))
        {
            subpath = subpath.Substring(basePath.Length);
        }

        return innerFileProvider.GetFileInfo(subpath);
    }

    /// <inheritdoc/>
    public IChangeToken Watch(string filter) => innerFileProvider.Watch(filter);
}
