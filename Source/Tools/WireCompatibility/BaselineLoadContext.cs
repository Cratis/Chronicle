// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// Loads one released contracts assembly in isolation from every other.
/// </summary>
/// <remarks>
/// Checking every minor of a major means loading thirty-odd assemblies that all call themselves
/// <c>Cratis.Chronicle.Contracts</c>. The default load context binds by simple name, so the second one would
/// silently resolve to the first and every baseline after the earliest would be compared against itself.
/// <para>
/// Only the contracts assembly is isolated. Everything it references - protobuf-net above all - is resolved from
/// the default context by returning null here, so the attribute types the schema generator reflects over are the
/// same types this process was compiled against. Isolating those too would make every <c>[ProtoContract]</c>
/// invisible to the generator.
/// </para>
/// </remarks>
sealed class BaselineLoadContext() : AssemblyLoadContext(isCollectible: true)
{
    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName) => null;
}
