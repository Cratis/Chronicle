// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Services.Compliance;

/// <summary>
/// The fully decided key-lifecycle targets, with no key state changed yet.
/// </summary>
/// <param name="Allowed">The authorized targets to mutate.</param>
/// <param name="NotCovered">The targets that must not be mutated.</param>
/// <param name="ListingFailed">Whether other eligible targets could not be listed.</param>
public record ComplianceKeyCoverage(IReadOnlyList<EventStoreName> Allowed, IReadOnlyList<EventStoreName> NotCovered, bool ListingFailed = false);
