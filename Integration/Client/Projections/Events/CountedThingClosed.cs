// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Events;

/// <summary>
/// A thing left the state it was counted in, without leaving the set.
/// </summary>
/// <remarks>
/// The event carries nothing. A projection reacting to it can only set a constant, which is exactly the
/// case a counting read model depends on and the case that used to be dropped.
/// </remarks>
[EventType]
public record CountedThingClosed;
