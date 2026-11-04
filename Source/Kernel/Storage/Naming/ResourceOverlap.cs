// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents a resource used by more than one owner.
/// </summary>
/// <param name="Identity">The shared identity.</param>
/// <param name="Owners">The distinct owners, where null stands for a name the kernel keeps for itself.</param>
public record ResourceOverlap(string Identity, IReadOnlyCollection<ResourceOwner?> Owners);
