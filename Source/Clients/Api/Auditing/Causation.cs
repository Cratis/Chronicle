// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Primitives;

namespace Cratis.Chronicle.Api.Auditing;

/// <summary>
/// Represents the payload for causation.
/// </summary>
/// <param name="Occurred">The time and date for when it occurred.</param>
/// <param name="Type">The type of cause.</param>
/// <param name="Properties">Properties associated with the causation.</param>
public record Causation(
    DateTimeOffset Occurred,
    string Type,
    IDictionary<string, string> Properties);
