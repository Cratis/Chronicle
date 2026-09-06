// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child line accumulating consumed capacity for one period.
/// </summary>
/// <param name="Period">The period, used as the child key.</param>
/// <param name="Consumed">The accumulated amount consumed within the period.</param>
public record UsagePeriod(
    string Period,

    [AddFrom<UsageRecorded>(nameof(UsageRecorded.Consumed))]
    decimal Consumed);
