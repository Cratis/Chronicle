// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>A versioned authoritative set of grants.</summary>
/// <param name="Version">The version of the grants.</param>
/// <param name="Grants">The grants.</param>
[GenerateSerializer]
public record StoreEntitlementsState([property: Id(0)] long Version, [property: Id(1)] IReadOnlyList<EventStoreAccessGrant> Grants);
