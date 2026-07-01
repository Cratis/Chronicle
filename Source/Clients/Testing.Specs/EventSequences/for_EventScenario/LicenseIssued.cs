// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Event carrying a <see cref="LicenseKey"/> (a <c>ConceptAs&lt;Guid&gt;</c>) constrained for uniqueness by <see cref="UniqueLicenseKey"/>.
/// </summary>
/// <param name="Key">The issued license key.</param>
[EventType]
public record LicenseIssued(LicenseKey Key);
