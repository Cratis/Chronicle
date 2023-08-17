// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Auditing;

/// <summary>
/// Represents a causation instance.
/// </summary>
/// <param name="Occurred">When it occurred.</param>
/// <param name="Type">Type of causation.</param>
/// <param name="Properties">Any properties associated with the causation.</param>
public record Causation(
    DateTimeOffset Occurred,
    CausationType Type,
    IImmutableDictionary<string, string> Properties);
