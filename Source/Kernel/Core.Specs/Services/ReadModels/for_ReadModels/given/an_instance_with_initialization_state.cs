// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.given;

public abstract class an_instance_with_initialization_state : all_dependencies
{
    protected ExpandoObject _instance;
    protected IDictionary<string, object?> _values;
    protected JsonObject _document;

    void Establish()
    {
        _readModelDefinition = _readModelDefinition with { Sink = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.SQL) };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        _instance = new ExpandoObject();
        _values = _instance;
        _values["name"] = "First";
        _values[WellKnownProperties.ReadModelInstanceInitialized] = true;
        _values[WellKnownProperties.LastHandledEventSequenceNumber] = 42L;
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(_instance);
        _sink.GetInstances(Arg.Any<ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>()).Returns(new ReadModelInstances([_instance], 1));
        _sink.ObserveInstances(Arg.Any<ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>()).Returns(Observable.Return<IEnumerable<ExpandoObject>>([_instance]));
        var converter = new ExpandoObjectConverter(new TypeFormats());
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => converter.ToJsonObject(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));
    }
}
