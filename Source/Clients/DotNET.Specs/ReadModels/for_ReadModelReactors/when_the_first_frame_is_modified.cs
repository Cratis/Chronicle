// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors;

public class when_the_first_frame_is_modified : given.watched_reactors
{
    void Because() => Emit(ReadModelChangeType.Modified);

    [Fact] void should_dispatch_modified_without_guessing_an_addition() => _changes.ShouldEqual([ReadModelChangeType.Modified]);
    [Fact] void should_preserve_the_event_context() => _contexts.Single().ShouldEqual(_context);
}
