// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// An event in a named-tag batch.
/// </summary>
/// <param name="EventType">The event type.</param>
/// <param name="Content">The serialized content.</param>
/// <param name="NamedTags">The named tags belonging to this event.</param>
/// <param name="Subject">The optional subject.</param>
public record EventToAppendWithNamedTags(EventType EventType, string Content, IEnumerable<NamedTag> NamedTags, string? Subject = null);
