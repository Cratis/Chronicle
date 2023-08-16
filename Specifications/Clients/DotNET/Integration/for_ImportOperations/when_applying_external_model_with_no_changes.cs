// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Integration.for_ImportOperations;

public class when_applying_external_model_with_no_changes : given.no_changes
{
    async Task Because() => await operations.Apply(incoming);

    [Fact] void should_not_append_any_events() => event_log.Verify(_ => _.AppendMany(IsAny<EventSourceId>(), IsAny<IEnumerable<EventAndValidFrom>>()), Never);
}
