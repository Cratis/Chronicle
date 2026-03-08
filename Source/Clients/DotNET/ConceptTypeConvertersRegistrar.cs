// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle;

/// <summary>
/// Ensures concept type converters are registered for assemblies setting up Chronicle.
/// </summary>
public static class ConceptTypeConvertersRegistrar
{
    static readonly HashSet<Assembly> _registeredAssemblies = [];

#if NET8_0
    static readonly object _sync = new();
#else
    static readonly Lock _sync = new();
#endif

    static ConceptTypeConvertersRegistrar() => EnsureFor(typeof(ConceptTypeConvertersRegistrar).Assembly);

    /// <summary>
    /// Ensures type converters are registered for the entry assembly.
    /// </summary>
    public static void EnsureForEntryAssembly()
    {
        EnsureFor(Assembly.GetEntryAssembly());
    }

    /// <summary>
    /// Ensures type converters are registered for the provided assembly.
    /// </summary>
    /// <param name="assembly">Assembly to register converters for.</param>
    public static void EnsureFor(Assembly? assembly)
    {
        if (assembly is null)
        {
            return;
        }

        if (_registeredAssemblies.Contains(assembly))
        {
            return;
        }
        lock (_sync)
        {
            if (!_registeredAssemblies.Add(assembly))
            {
                return;
            }
        }

        assembly.RegisterTypeConvertersForConcepts();
    }
}
