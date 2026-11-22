// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Setup.Serialization.for_KernelSerializers.given;

/// <summary>
/// Reads the kernel types that travel between grains from <c>GrainPayloadContracts.txt</c>.
/// </summary>
public static class PayloadManifest
{
    static readonly string[] _structuredKinds = ["json", "orleans"];

    /// <summary>
    /// Gets the kernel types in the manifest that are written as structures, which is what a serializer for a payload type has to carry.
    /// </summary>
    /// <returns>The resolved types, and the manifest names that could not be resolved.</returns>
    public static (IReadOnlyList<Type> Types, IReadOnlyList<string> Unresolved) Read()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "GrainPayloadContracts.txt");
        var assemblies = LoadKernelAssemblies();
        var types = new List<Type>();
        var unresolved = new List<string>();

        foreach (var line in File.ReadLines(path))
        {
            var separator = line.IndexOf(' ');
            if (line.StartsWith(' ') || line.StartsWith('#') || separator < 0)
            {
                continue;
            }

            var kind = line[..separator];
            var rest = line[(separator + 1)..];
            if (!_structuredKinds.Contains(kind))
            {
                continue;
            }

            var name = rest.StartsWith('[') ? rest[(rest.IndexOf("] ", StringComparison.Ordinal) + 2)..] : rest;
            if (!name.StartsWith("Cratis.Chronicle.", StringComparison.Ordinal) || name.Contains('<'))
            {
                continue;
            }

            var type = assemblies.Select(_ => _.GetType(name, false)).FirstOrDefault(_ => _ is not null);
            if (type is null)
            {
                unresolved.Add(name);
            }
            else
            {
                types.Add(type);
            }
        }

        return (types, unresolved);
    }

    static Assembly[] LoadKernelAssemblies()
    {
        _ = typeof(EventType).Assembly;
        _ = typeof(ObserverState).Assembly;
        _ = typeof(SerializationConfigurationExtensions).Assembly;
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(_ => _.GetName().Name?.StartsWith("Cratis.Chronicle", StringComparison.Ordinal) ?? false)
            .ToArray();
    }
}
