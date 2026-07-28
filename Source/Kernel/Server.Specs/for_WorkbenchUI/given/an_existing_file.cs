// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;

namespace Cratis.Chronicle.Server.for_WorkbenchUI.given;

/// <summary>
/// An <see cref="IFileInfo"/> for a file that exists - the counterpart to <see cref="NotFoundFileInfo"/>,
/// carrying only what resolution looks at.
/// </summary>
/// <param name="name">The name of the file.</param>
public class an_existing_file(string name) : IFileInfo
{
    public bool Exists => true;

    public bool IsDirectory => false;

    public DateTimeOffset LastModified => DateTimeOffset.MinValue;

    public long Length => 0;

    public string Name => name;

    public string? PhysicalPath => null;

    public Stream CreateReadStream() => new MemoryStream();
}
