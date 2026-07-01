// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Second event type sharing the <see cref="UniqueLicenseKey"/> constraint on its <see cref="LicenseKey"/> (a <c>ConceptAs&lt;Guid&gt;</c>) property.
/// </summary>
/// <param name="Key">The reissued license key.</param>
[EventType]
public record LicenseReissued(LicenseKey Key);
