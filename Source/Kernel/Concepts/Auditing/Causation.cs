// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Auditing;

/// <summary>
/// Represents a causation instance.
/// </summary>
/// <param name="Occurred">When it occurred.</param>
/// <param name="Type">Type of causation.</param>
/// <param name="Properties">Any properties associated with the causation.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Auditing.Causation")]
public record Causation(
    [Id(0)] DateTimeOffset Occurred,
    [Id(1)] CausationType Type,
    [Id(2)] IDictionary<string, string> Properties);
