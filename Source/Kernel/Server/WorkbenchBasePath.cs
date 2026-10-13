// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Represents the handling of the path the Workbench is served under.
/// </summary>
/// <remarks>
/// The Workbench frontend reads the <c language="csharp">base-path</c> meta tag of <c language="csharp">index.html</c> and uses it for routing and as the base
/// path of every API call, but the page ships with root-absolute asset references and an empty meta tag. Serving it under a
/// prefix therefore means rewriting those on the way out, which is what <see cref="Rewrite"/> does - the same rewrite the
/// standalone Workbench host and the Cratis Stage perform.
/// </remarks>
public static class WorkbenchBasePath
{
    const string BasePathMetaPattern = "name=\"base-path\" content=\"\"";

    /// <summary>
    /// Normalize a configured base path to either empty, for the root, or a path with a leading and no trailing slash.
    /// </summary>
    /// <param name="configured">The configured value, if any.</param>
    /// <returns>The normalized base path.</returns>
    public static string Normalize(string? configured)
    {
        var trimmed = configured?.Trim().Trim('/') ?? string.Empty;
        return trimmed.Length == 0 ? string.Empty : $"/{trimmed}";
    }

    /// <summary>
    /// Rewrite the Workbench entry page so it resolves against a base path.
    /// </summary>
    /// <param name="html">The Workbench <c language="csharp">index.html</c> as it ships.</param>
    /// <param name="basePath">The normalized base path, see <see cref="Normalize"/>.</param>
    /// <returns>The page to serve. Unchanged when the base path is the root.</returns>
    public static string Rewrite(string html, string basePath) =>
        basePath.Length == 0
            ? html
            : html
                .Replace(BasePathMetaPattern, $"name=\"base-path\" content=\"{basePath}\"", StringComparison.Ordinal)
                .Replace("src=\"/", $"src=\"{basePath}/", StringComparison.Ordinal)
                .Replace("href=\"/", $"href=\"{basePath}/", StringComparison.Ordinal);

    /// <summary>
    /// Wrap a file provider so the Workbench entry page it serves is rewritten for a base path.
    /// </summary>
    /// <param name="inner">The <see cref="IFileProvider"/> holding the Workbench UI.</param>
    /// <param name="basePath">The normalized base path, see <see cref="Normalize"/>.</param>
    /// <returns>A provider serving the rewritten entry page, or <paramref name="inner"/> itself when the base path is the root.</returns>
    public static IFileProvider Apply(IFileProvider inner, string basePath) =>
        basePath.Length == 0 ? inner : new RewritingFileProvider(inner, basePath);

    sealed class RewritingFileProvider(IFileProvider inner, string basePath) : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath) => inner.GetDirectoryContents(subpath);

        public IChangeToken Watch(string filter) => inner.Watch(filter);

        public IFileInfo GetFileInfo(string subpath)
        {
            var file = inner.GetFileInfo(subpath);
            var isEntryPoint = string.Equals(subpath.TrimStart('/'), WorkbenchUI.EntryPoint, StringComparison.OrdinalIgnoreCase);
            return file.Exists && isEntryPoint ? new RewrittenEntryPoint(file, basePath) : file;
        }
    }

    sealed class RewrittenEntryPoint : IFileInfo
    {
        readonly IFileInfo _original;
        readonly byte[] _content;

        public RewrittenEntryPoint(IFileInfo original, string basePath)
        {
            _original = original;

            using var stream = original.CreateReadStream();
            using var reader = new StreamReader(stream);
            _content = System.Text.Encoding.UTF8.GetBytes(Rewrite(reader.ReadToEnd(), basePath));
        }

        public bool Exists => true;

        public long Length => _content.Length;

        public string? PhysicalPath => null;

        public string Name => _original.Name;

        public DateTimeOffset LastModified => _original.LastModified;

        public bool IsDirectory => false;

        public Stream CreateReadStream() => new MemoryStream(_content, writable: false);
    }
}
