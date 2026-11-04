// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_classifying_a_write_cycle.given;

/// <summary>
/// Classifies changes as a single write or a multi-write cycle, and measures what the classification allocates.
/// </summary>
public abstract class a_write_cycle_classification : Specification
{
    protected bool _result;
    protected long _allocated;

    protected static PropertiesChanged<System.Dynamic.ExpandoObject> Direct() =>
        new(new System.Dynamic.ExpandoObject(), [new PropertyDifference("group", null, "g")]);

    protected static Joined Join() =>
        new(new System.Dynamic.ExpandoObject(), "J", "joinId", ArrayIndexers.NoIndexers, [Direct()]);

    protected static ChildRemovedFromAll ChildRemoval() =>
        new("children", "id", "c1", ArrayIndexers.NoIndexers);

    protected static Change[] JoinAndDirect() => [Join(), Direct()];

    /// <summary>
    /// Classifies the changes once to warm up, then many times, and records the bytes the current thread allocated.
    /// </summary>
    /// <param name="changes">The changes to classify.</param>
    protected void ClassifyAndMeasure(IEnumerable<Change> changes)
    {
        _result = Sink.IsMultiWrite(changes);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            Sink.IsMultiWrite(changes);
        }

        _allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
