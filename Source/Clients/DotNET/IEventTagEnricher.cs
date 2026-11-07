// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Defines a system that contributes named tags to events as they are appended.
/// </summary>
/// <remarks>
/// Implementations are discovered automatically (and resolved through dependency injection); every enricher runs for every
/// appended event, on every append path including units of work. They must be pure and synchronous, and are additive only:
/// they can add tags but never remove or change tags that are already there.
/// </remarks>
public interface IEventTagEnricher
{
    /// <summary>
    /// Gets the named tags to add to the event described by the context.
    /// </summary>
    /// <param name="context">The <see cref="EventTagContext"/> for the event.</param>
    /// <returns>The tags to add; empty when there is nothing to add.</returns>
    IEnumerable<NamedTag> GetTags(EventTagContext context);
}
