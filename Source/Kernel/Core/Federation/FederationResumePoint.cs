// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Where a consumer resumes: the origin and source sequence number of the last event forwarded into the local inbox.
/// </summary>
/// <param name="OriginEventStore">The event store the last forwarded event originated in.</param>
/// <param name="LastSourceSequenceNumber">The sequence number of that event in the origin.</param>
public record FederationResumePoint(string OriginEventStore, ulong LastSourceSequenceNumber);
