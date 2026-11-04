// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents the outcome of the check of the names in use.
/// </summary>
/// <param name="QuarantinedStores">The quarantined event stores.</param>
/// <param name="QuarantinedNamespaces">The quarantined namespaces.</param>
/// <param name="Overlaps">The overlaps found.</param>
public record ResourceNameAuditResult(
    IReadOnlyCollection<string> QuarantinedStores,
    IReadOnlyCollection<ResourceOwner> QuarantinedNamespaces,
    IReadOnlyCollection<ResourceOverlap> Overlaps);
