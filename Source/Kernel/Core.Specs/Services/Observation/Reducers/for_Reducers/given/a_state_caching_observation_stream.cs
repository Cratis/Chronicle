// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.given;

public class a_state_caching_observation_stream : an_observation_stream
{
    /// <summary>The state the converter hands back for any result the client sends.</summary>
    protected ExpandoObject _stateReturnedByClient = StateOf("first", 1);

    protected override StateCaching? StateCaching => new() { Enabled = true };

    protected static ExpandoObject StateOf(string name, int count, bool nameFirst = true)
    {
        var state = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)state;
        if (nameFirst)
        {
            dictionary["name"] = name;
            dictionary["count"] = count;
        }
        else
        {
            dictionary["count"] = count;
            dictionary["name"] = name;
        }

        return state;
    }

    protected override IExpandoObjectConverter CreateExpandoObjectConverter()
    {
        var converter = Substitute.For<IExpandoObjectConverter>();
        converter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(_ => _stateReturnedByClient);
        converter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(_ => new JsonObject { ["name"] = "serialized" });
        converter.ToExpandoObject(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<JsonSchema>()).Returns(_ => _stateReturnedByClient);
        converter.When(_ => _.WriteJson(Arg.Any<Utf8JsonWriter>(), Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()))
            .Do(call => call.Arg<Utf8JsonWriter>().WriteStartObject());
        return converter;
    }

    protected Task<ReducerSubscriberResult> Deliver(ExpandoObject? initialState, string partition = "partition")
    {
        var completion = new TaskCompletionSource<ReducerSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _deliver(new(partition, [], initialState), completion);
        return completion.Task;
    }

    protected void ReplyWithState(ExpandoObject state, string partition = "partition", ObservationState observationState = ObservationState.Success)
    {
        _stateReturnedByClient = state;
        var deliveryId = _deliveries[^1].DeliveryId;
        var hasState = observationState == ObservationState.Success;
        _messages.OnNext(new(new(PageDelivery?.Enabled == true
            ? new ReducerResult
            {
                DeliveryId = deliveryId,
                Partitions =
                [
                    new PartitionResult
                    {
                        Partition = partition,
                        State = observationState,
                        LastSuccessfulObservation = 12,
                        ExceptionStackTrace = string.Empty,
                        ReadModelState = hasState ? "{}" : null
                    }
                ]
            }
            : new ReducerResult
            {
                DeliveryId = deliveryId,
                Partition = partition,
                State = observationState,
                LastSuccessfulObservation = 12,
                ExceptionStackTrace = string.Empty,
                ReadModelState = hasState ? "{}" : null
            })));
    }

    protected bool Unchanged(int delivery) => PageDelivery?.Enabled == true
        ? _deliveries[delivery].Partitions.Single().InitialStateUnchanged
        : _deliveries[delivery].InitialStateUnchanged;

    protected string? InitialStateSent(int delivery) => PageDelivery?.Enabled == true
        ? _deliveries[delivery].Partitions.Single().InitialState
        : _deliveries[delivery].InitialState;
}
