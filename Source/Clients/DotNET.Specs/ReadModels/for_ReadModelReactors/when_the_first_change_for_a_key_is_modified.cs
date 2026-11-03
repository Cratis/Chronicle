// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors;

public class when_the_first_change_for_a_key_is_modified : given.watched_reactors
{
    async Task Because() => await Send(ReadModelChangeType.Modified);

    [Fact] void should_dispatch_only_modified() => _recorded.Select(_ => _.Type).ShouldEqual([ReadModelChangeType.Modified]);
    [Fact] void should_preserve_the_causing_event_context() => _recorded.Single().Context.ShouldEqual(_context);
}
