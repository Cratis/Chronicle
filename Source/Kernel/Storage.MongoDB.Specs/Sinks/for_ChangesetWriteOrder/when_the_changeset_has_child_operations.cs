// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetWriteOrder;

public class when_the_changeset_has_child_operations : Specification
{
    Change[] _changes;
    IReadOnlyList<IReadOnlyList<Change>>? _result;

    void Establish()
    {
        _changes = [Changes.Direct("count"), new ChildAdded(new ExpandoObject(), "children", "id", "c", ArrayIndexers.NoIndexers), Changes.Join(true, "count")];
    }

    void Because() => _result = ChangesetWriteOrder.SegmentsFor(_changes);

    [Fact] void should_use_the_single_pass() => _result.ShouldBeNull();
}
