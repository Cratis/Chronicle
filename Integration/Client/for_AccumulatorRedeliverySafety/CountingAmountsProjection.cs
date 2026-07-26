// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety;

/// <summary>
/// Counts events onto a document keyed by the event's own event source id, which is the class the watermark guard
/// covers.
/// </summary>
public class CountingAmountsProjection : IProjectionFor<CountedAmounts>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<CountedAmounts> builder) => builder
        .From<AmountRecorded>(_ => _.Count(m => m.Handled));
}
