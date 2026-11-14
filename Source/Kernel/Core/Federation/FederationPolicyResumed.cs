// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The suspended policy of a subject was resumed. The event source id is the subject.
/// </summary>
/// <param name="ResumedBy">The identity that resumed the policy.</param>
[EventType]
public record FederationPolicyResumed(string ResumedBy);
