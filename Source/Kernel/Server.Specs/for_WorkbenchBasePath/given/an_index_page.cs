// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.Server.for_WorkbenchBasePath.given;

/// <summary>
/// A file provider holding the Workbench entry page as it ships - root-absolute references and an empty base path - and
/// one asset next to it.
/// </summary>
public class an_index_page : IFileProvider
{
    public const string Page = "<html><head><meta name=\"base-path\" content=\"\" /><link href=\"/assets/app.css\" /></head><body><script src=\"/assets/app.js\"></script></body></html>";

    public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

    public IFileInfo GetFileInfo(string subpath) => subpath.TrimStart('/') switch
    {
        WorkbenchUI.EntryPoint => new StringFile(WorkbenchUI.EntryPoint, Page),
        "assets/app.js" => new StringFile("app.js", "console.log('\"/\"')"),
        _ => new NotFoundFileInfo(subpath)
    };

    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    public static string Read(IFileInfo file)
    {
        using var stream = file.CreateReadStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    sealed class StringFile(string name, string content) : IFileInfo
    {
        public bool Exists => true;

        public long Length => Encoding.UTF8.GetByteCount(content);

        public string? PhysicalPath => null;

        public string Name => name;

        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

        public bool IsDirectory => false;

        public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}
