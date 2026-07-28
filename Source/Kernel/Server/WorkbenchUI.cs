// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Represents the resolution of where the Workbench UI assets are served from.
/// </summary>
/// <remarks>
/// The Workbench UI reaches a deployment in one of two ways, and the Kernel serves whichever is present:
/// embedded into the Cratis.Chronicle.Workbench assembly - which only happens when the frontend output
/// existed when that assembly was compiled - or as files in the web root next to the binary, which is how
/// the container images ship it. Neither is guaranteed: a Kernel-only deployment has no Workbench UI at
/// all, so resolution reports the absence instead of failing and the server keeps running without the UI.
/// </remarks>
public static class WorkbenchUI
{
    /// <summary>
    /// The file the Workbench UI is entered through.
    /// </summary>
    /// <remarks>
    /// Doubles as the marker for whether a file provider actually holds the UI - the providers considered
    /// here exist for other reasons too, so their mere presence says nothing.
    /// </remarks>
    public const string EntryPoint = "index.html";

    const string EmbeddedManifestResourceName = "Microsoft.Extensions.FileProviders.Embedded.Manifest.xml";

    /// <summary>
    /// Resolve the <see cref="IFileProvider"/> for the Workbench UI embedded into an assembly.
    /// </summary>
    /// <param name="workbenchAssembly">The <see cref="Assembly"/> the Workbench UI can be embedded into.</param>
    /// <param name="filesRoot">The root the Workbench UI files are embedded under.</param>
    /// <returns>The <see cref="IFileProvider"/> for the embedded UI, or null when this build has none embedded.</returns>
    public static IFileProvider? ResolveEmbedded(Assembly workbenchAssembly, string filesRoot)
    {
        // ManifestEmbeddedFileProvider throws when the assembly carries no manifest, which is exactly what
        // a build that did not have the frontend output available produces - so check before constructing.
        if (!workbenchAssembly.GetManifestResourceNames().Contains(EmbeddedManifestResourceName))
        {
            return null;
        }

        return new ManifestEmbeddedFileProvider(workbenchAssembly, filesRoot);
    }

    /// <summary>
    /// Resolve the <see cref="IFileProvider"/> to serve the Workbench UI from.
    /// </summary>
    /// <param name="embedded">The <see cref="IFileProvider"/> for the embedded UI, or null when nothing is embedded.</param>
    /// <param name="webRoot">The <see cref="IFileProvider"/> for the web root the UI can be deployed to.</param>
    /// <returns>The <see cref="IFileProvider"/> to serve from, or null when the UI is not part of this deployment.</returns>
    public static IFileProvider? Resolve(IFileProvider? embedded, IFileProvider webRoot)
    {
        var candidates = new[] { embedded, webRoot }
            .Where(candidate => candidate is not null && Holds(candidate))
            .Select(candidate => candidate!)
            .ToArray();

        return candidates.Length switch
        {
            0 => null,
            1 => candidates[0],
            _ => new CompositeFileProvider(candidates)
        };
    }

    static bool Holds(IFileProvider provider) => provider.GetFileInfo(EntryPoint).Exists;
}
