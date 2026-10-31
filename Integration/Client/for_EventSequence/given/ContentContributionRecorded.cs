// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// A contribution to a document whose key differs from the contributing event source.
/// </summary>
/// <param name="DocumentId">The target document.</param>
/// <param name="Amount">The contributed amount.</param>
[EventType]
public record ContentContributionRecorded(string DocumentId, int Amount);
