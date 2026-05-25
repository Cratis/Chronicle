// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the definition of a capture source.
/// </summary>
/// <param name="Type">The <see cref="SourceType"/>.</param>
/// <param name="Url">Optional URL for API sources.</param>
/// <param name="Poll">Optional poll interval for API sources.</param>
/// <param name="Auth">Optional authentication configuration.</param>
/// <param name="Path">Optional path for webhook sources.</param>
/// <param name="Topic">Optional topic for message sources.</param>
public record SourceDefinition(
    SourceType Type,
    string? Url = default,
    string? Poll = default,
    string? Auth = default,
    string? Path = default,
    string? Topic = default);
