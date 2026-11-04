// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeRegistryCache.given;

/// <summary>
/// Serializes the specs that observe a collectible assembly being unloaded.
/// </summary>
/// <remarks>
/// Whether an unloaded context is collected is a process-global observation. Type discovery in
/// Cratis.Fundamentals records every assembly in <see cref="AppDomain.GetAssemblies"/> in a static set
/// when it walks the assembly closure, so a spec running in parallel that triggers discovery can pin a
/// collectible assembly created by one of these specs for the rest of the process. Running them without
/// parallel collections keeps that walk from observing their assemblies.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class UnloadingCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "Unloading collectible assemblies";
}
