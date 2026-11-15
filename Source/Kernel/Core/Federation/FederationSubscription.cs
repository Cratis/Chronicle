// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// What a local event store subscription asks for from a federation source.
/// </summary>
/// <param name="EventTypeIds">The ids of the event types it subscribes to.</param>
/// <param name="NamedTags">Its named tag filter, empty for none.</param>
public record FederationSubscription(IReadOnlyCollection<string> EventTypeIds, IReadOnlyCollection<NamedTag> NamedTags);
