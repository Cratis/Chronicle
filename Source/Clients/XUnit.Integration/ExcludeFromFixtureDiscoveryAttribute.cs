// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Marks an artifact that a specification registers explicitly, so a fixture that discovers artifacts from the
/// test assembly leaves it out.
/// </summary>
/// <remarks>
/// An artifact that deliberately shares its identity with another artifact - the same reactor as a different version
/// of a service would register it - cannot be registered next to that artifact, which is what discovery would do.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class ExcludeFromFixtureDiscoveryAttribute : Attribute;
