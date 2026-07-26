// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety;

/// <summary>
/// Counts events onto a single document shared by every event source, which is the class the watermark guard must
/// never cover: its per-document event stream is deliberately not monotonic.
/// </summary>
public class CollapsingCountProjection : IProjectionFor<CollapsedCount>
{
    /// <summary>
    /// The constant key every event is collapsed onto.
    /// </summary>
    public const string ConstantKeyValue = "collapsed";

    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<CollapsedCount> builder) => builder
        .From<AmountRecorded>(_ => _
            .UsingConstantKey(ConstantKeyValue)
            .Count(m => m.Handled));
}
