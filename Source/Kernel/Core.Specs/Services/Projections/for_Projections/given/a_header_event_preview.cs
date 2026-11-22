// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_Projections.given;

public class a_header_event_preview : all_dependencies
{
    protected AppendedEvent[] _stored;
    protected AppendedEvent[] _previewed;
    protected string[] _entries;

    void Establish()
    {
        _stored = header_events.All();
        _projectionDefinition = _projectionDefinition with { SubscribesToAllEvents = true };
        SetCompiledDefinition(_projectionDefinition);
        _eventCursor.MoveNext().Returns(true, false);
        _eventCursor.Current.Returns(_stored);
        _readModelsCompliance.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<ExpandoObject>())
            .Returns(call => call.Arg<ExpandoObject>());
        _projectionGrain.Process(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(call => CopyIntoState(call.Arg<IEnumerable<AppendedEvent>>()));
        _projectionGrain.ProcessForPreview(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<Concepts.ReadModels.ReadModelDefinition>()).Returns(call => CopyIntoState(call.Arg<IEnumerable<AppendedEvent>>()));
    }

    ExpandoObject[] CopyIntoState(IEnumerable<AppendedEvent> events)
    {
        _previewed = events.ToArray();
        dynamic state = new ExpandoObject();
        state.copied = JsonSerializer.Serialize(_previewed);
        return [(ExpandoObject)state];
    }
}
