// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a utility class to locate the test assembly.
/// </summary>
public static class TestAssemblyLocator
{
    /// <summary>
    /// Gets the assembly that holds the tests currently being run.
    /// </summary>
    /// <returns>Possible <see cref="Assembly"/>, null if not found.</returns>
    public static Assembly? GetTestAssembly()
    {
        var stackTrace = new StackTrace();

        foreach (var frame in stackTrace.GetFrames() ?? [])
        {
            var method = frame.GetMethod();
            var assembly = method?.DeclaringType?.Assembly;

            if (assembly == null) continue;

            // Skip dynamic/system/library assemblies
            if (assembly.IsDynamic) continue;
            if (assembly.FullName?.StartsWith("System") == true) continue;
            if (assembly == typeof(TestAssemblyLocator).Assembly) continue;

            return assembly;
        }

        return null;
    }
}
