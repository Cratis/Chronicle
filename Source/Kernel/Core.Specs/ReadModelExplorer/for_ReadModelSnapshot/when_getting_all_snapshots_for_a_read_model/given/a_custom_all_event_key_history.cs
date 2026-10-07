// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model.given;

public class a_custom_all_event_key_history : a_mixed_all_event_history
{
    void Establish()
    {
        _definition = _definition with { FromEvery = new FromEveryDefinition(_definition.FromEvery.Properties, false) { Key = "modelKey" } };
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["modelKey"] = "my-instance";
        _events[1] = new AppendedEvent(_events[1].Context with { EventSourceId = "another-source" }, content);
        _projection.GetEventsForKey(Arg.Any<Concepts.EventStoreNamespaceName>(), "my-instance", Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call => call.Arg<IEnumerable<AppendedEvent>>().Where(@event =>
                @event.Context.EventSourceId == "my-instance" ||
                (((IDictionary<string, object?>)@event.Content).TryGetValue("modelKey", out var key) && key as string == "my-instance")));
    }
}
