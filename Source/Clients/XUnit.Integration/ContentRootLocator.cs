// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Finds the content root of a test assembly: the closest directory at or above it that holds a project file.
/// </summary>
internal static class ContentRootLocator
{
    /// <summary>
    /// Find the content root for a test assembly.
    /// </summary>
    /// <param name="testAssembly">The test assembly.</param>
    /// <returns>The closest directory at or above the assembly location that holds a project file.</returns>
    /// <exception cref="ContentRootNotFound">Thrown when no directory at or above the assembly holds a project file.</exception>
    internal static ContentRoot FindFor(Assembly testAssembly)
    {
        var directory = Path.GetDirectoryName(new Uri(testAssembly.Location).LocalPath);
        while (directory is not null && !Directory.EnumerateFiles(directory, "*.csproj").Any())
        {
            directory = Path.GetDirectoryName(directory);
        }

        return directory ?? throw new ContentRootNotFound(testAssembly.Location);
    }
}
