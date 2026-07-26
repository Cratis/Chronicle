// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

/// <summary>
/// An event whose projection key is read from its content rather than from the event source it belongs to, so
/// events from many event sources collapse onto one read model document.
/// </summary>
/// <param name="Group">The group the event belongs to - the read model key.</param>
[EventType]
public record CollapsedEvent(string Group);
