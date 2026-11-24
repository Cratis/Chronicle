// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors;

public class when_restarting_before_a_modification : given.watched_reactors
{
    void Establish()
    {
        Emit(ReadModelChangeType.Added);
        _changes.Clear();
        _reactors.Stop();
        _reactors.Start();
    }

    void Because() => Emit(ReadModelChangeType.Modified);

    [Fact] void should_keep_the_modified_change_type() => _changes.ShouldEqual([ReadModelChangeType.Modified]);
}
