// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Types;

/// <summary>
/// Represents helpers for working with assemblies.
/// </summary>
public static class AssemblyHelpers
{
    static AssemblyHelpers()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => ResolveFromFile(e.Name);
    }

    /// <summary>
    /// Resolves an assembly from a name.
    /// </summary>
    /// <param name="name">Name of assembly to resolve.</param>
    /// <returns>Assembly instance of found, null if not.</returns>
    public static Assembly? Resolve(string name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch
        {
            return null!;
        }
    }

    /// <summary>
    /// Resolves an assembly from a file based on a name.
    /// </summary>
    /// <param name="name">Name of assembly to resolve.</param>
    /// <returns>Assembly instance of found, null if not.</returns>
    public static Assembly? ResolveFromFile(string name)
    {
        try
        {
            var assemblyName = new AssemblyName(name);
            var file = $"{assemblyName.Name}.dll";
            var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, file);
            if (File.Exists(path))
            {
                return Assembly.LoadFile(path);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
