// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Collections;
using Cratis.ProxyGenerator.Templates;
using HandlebarsDotNet;

namespace Cratis.ProxyGenerator;

/// <summary>
/// Extension methods for <see cref="IDescriptor"/>.
/// </summary>
public static class DescriptorExtensions
{
    /// <summary>
    /// Write the descriptors to disk.
    /// </summary>
    /// <param name="descriptors">Descriptors to write.</param>
    /// <param name="targetPath">The target path to write relative to.</param>
    /// <param name="typesInvolved">Collection of types that are involved from any of the types written.</param>
    /// <param name="template">Template to use for writing.</param>
    /// <param name="directories">All directories that has been written to.</param>
    /// <param name="typeNameToEcho">The type name to echo for statistics.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task Write(
        this IEnumerable<IDescriptor> descriptors,
        string targetPath,
        IList<Type> typesInvolved,
        HandlebarsTemplate<object, object> template,
        IList<string> directories,
        string typeNameToEcho)
    {
        var stopwatch = Stopwatch.StartNew();
        foreach (var descriptor in descriptors)
        {
            var path = descriptor.Type.ResolveTargetPath();
            var fullPath = Path.Join(targetPath, path, $"{descriptor.Name}.ts");
            var directory = Path.GetDirectoryName(fullPath)!;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            if (!directories.Contains(directory))
            {
                directories.Add(directory);
            }

            var proxyContent = template(descriptor);
            await File.WriteAllTextAsync(fullPath, proxyContent);
        }

        descriptors.SelectMany(_ => _.TypesInvolved).ForEach(typesInvolved.Add);

        Console.WriteLine($"{descriptors.Count()} {typeNameToEcho} in {stopwatch.Elapsed}");
    }
}
