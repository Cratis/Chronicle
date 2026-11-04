// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents one resource in use, identified the way its provider identifies it.
/// </summary>
/// <param name="Owner">The owner, or null for a name the kernel keeps for itself.</param>
/// <param name="Kind">The <see cref="ResourceKind"/>.</param>
/// <param name="Identity">The provider-canonical identity. Resources with the same identity are the same resource.</param>
public record ResourceUse(ResourceOwner? Owner, ResourceKind Kind, string Identity);
