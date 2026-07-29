// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the definition of a capture source.
/// </summary>
/// <param name="Type">The <see cref="SourceType"/>.</param>
/// <param name="Api">Optional API name for API sources.</param>
/// <param name="Poll">Optional poll interval for API sources.</param>
/// <param name="Route">Optional route for API sources.</param>
/// <param name="Path">Optional path for webhook sources.</param>
/// <param name="Topic">Optional topic for message sources.</param>
/// <param name="Authorization">
/// Optional authentication configuration. This is configured in code when the source is configured,
/// not through the Capture Declaration Language, so that secrets never live in capture text.
/// </param>
public record SourceDefinition(
    SourceType Type,
    string? Api = default,
    string? Poll = default,
    string? Route = default,
    string? Path = default,
    string? Topic = default,
    SourceAuthorization? Authorization = default);
