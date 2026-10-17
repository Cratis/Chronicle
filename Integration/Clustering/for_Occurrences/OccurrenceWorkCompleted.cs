// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

/// <summary>
/// A sample application's durable business effect, used to count committed work independently of receipts.
/// </summary>
/// <param name="Value">The sample application's result.</param>
[EventType]
public record OccurrenceWorkCompleted(string Value);
