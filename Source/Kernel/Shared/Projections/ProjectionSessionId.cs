// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents a session for a projection, typically used when asking to project immediately.
/// </summary>
/// <param name="Value">Inner value.</param>
public record ProjectionSessionId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ProjectionSessionId"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator ProjectionSessionId(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="CorrelationId"/> to <see cref="ProjectionSessionId"/>.
    /// </summary>
    /// <param name="correlationId"><see cref="CorrelationId"/> to convert from.</param>
    public static implicit operator ProjectionSessionId(CorrelationId correlationId) => new(correlationId.Value);

    /// <summary>
    /// Implicitly convert from <see cref="ProjectionSessionId"/> to <see cref="CorrelationId"/>.
    /// </summary>
    /// <param name="sessionId"><see cref="ProjectionSessionId"/> to convert from.</param>
    public static implicit operator CorrelationId(ProjectionSessionId sessionId) => new(sessionId.Value);
}
