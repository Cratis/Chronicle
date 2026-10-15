// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies the occurrence and its ingress outcome.
/// </summary>
/// <param name="Id">The stable occurrence identity.</param>
/// <param name="Status">The recording outcome.</param>
[GenerateSerializer]
public record OccurrenceIngressResult([property: Id(0)] string Id, [property: Id(1)] OccurrenceIngressStatus Status);
