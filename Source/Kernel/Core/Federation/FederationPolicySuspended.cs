// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The policy of a subject was suspended. The event source id is the subject.
/// </summary>
/// <param name="SuspendedBy">The identity that suspended the policy.</param>
[EventType]
public record FederationPolicySuspended(string SuspendedBy);
