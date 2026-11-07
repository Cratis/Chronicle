// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle;

/// <summary>
/// Stamps the ambient tags opened through <see cref="EventTags"/> on events.
/// </summary>
/// <remarks>
/// The audience is only stamped on the outbox or on event types marked with <see cref="AudienceAttribute"/>.
/// </remarks>
internal sealed class AmbientEventTagEnricher : IEventTagEnricher
{
    /// <inheritdoc/>
    public IEnumerable<NamedTag> GetTags(EventTagContext context)
    {
        var ambient = EventTags.Current;
        if (ambient.Count == 0) return [];

        var carriesAudience = context.EventSequenceId == EventSequenceId.Outbox || EventTagResolver.RequiresAudience(context.Event.GetType());
        return ambient.Where(tag => carriesAudience || tag.Name.Value != WellKnownTags.Audience).ToArray();
    }
}
