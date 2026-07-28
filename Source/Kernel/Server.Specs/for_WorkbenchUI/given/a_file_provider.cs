// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.Server.for_WorkbenchUI.given;

/// <summary>
/// An <see cref="IFileProvider"/> holding exactly the files it is given, so specs can express what each
/// candidate location does and does not contain without touching the file system.
/// </summary>
/// <param name="files">The files the provider holds.</param>
public class a_file_provider(params string[] files) : IFileProvider
{
    public static a_file_provider Empty() => new();

    public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

    public IFileInfo GetFileInfo(string subpath) =>
        files.Contains(subpath) ? new an_existing_file(subpath) : new NotFoundFileInfo(subpath);

    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
}
