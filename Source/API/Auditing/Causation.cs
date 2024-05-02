// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.API.Auditing;

/// <summary>
/// Represents a causation instance.
/// </summary>
/// <param name="Occurred">When it occurred.</param>
/// <param name="Type">Type of causation.</param>
/// <param name="Properties">Any properties associated with the causation.</param>
public record Causation(
    DateTimeOffset Occurred,
    string Type,
    IDictionary<string, string> Properties);
