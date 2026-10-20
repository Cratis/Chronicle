// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Records a durable delivery deadline independently of the failure attempt budget.
/// </summary>
/// <param name="Id">Identity used to fence callbacks from earlier deferrals.</param>
/// <param name="Reason">Why delivery is deferred.</param>
/// <param name="Since">When the deferral began, in UTC.</param>
/// <param name="Until">The earliest next delivery time, in UTC.</param>
/// <param name="ResumeFrom">The first event not yet handled, inclusive.</param>
public record PartitionDeferral(Guid Id, PartitionDeferralReason Reason, DateTimeOffset Since, DateTimeOffset Until, EventSequenceNumber ResumeFrom);
