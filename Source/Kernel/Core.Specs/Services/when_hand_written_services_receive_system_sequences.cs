// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Grpc.Core;
using ProtoBuf.Grpc;
using static Cratis.Chronicle.Services.given.hand_written_routing_surface;

namespace Cratis.Chronicle.Services;

public class when_hand_written_services_receive_system_sequences : for_QueryExecutor.given.a_routed_query
{
    readonly Dictionary<string, List<Exception>> _errors = [];
    readonly List<Exception> _administratorErrors = [];
    Dictionary<string, Func<string, CallContext, Task>> _requests;

    void Establish()
    {
        var projections = (Contracts.Projections.IProjections)Create(typeof(Projections.Projections));
        var readModels = (Contracts.ReadModels.IReadModels)Create(typeof(ReadModels.ReadModels));
        _requests = new()
        {
            ["Register"] = (sequence, context) => projections.Register(
                new()
                {
                    EventStore = "store",
                    Projections = [new() { Identifier = "projection", ReadModel = "read-model", EventSequenceId = sequence }]
                },
                context),
            ["Preview"] = async (sequence, context) => await projections.Preview(new() { EventStore = "store", Namespace = "namespace", EventSequenceId = sequence }, context),
            ["GetInstanceByKey"] = async (sequence, context) => await readModels.GetInstanceByKey(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "read-model", EventSequenceId = sequence }, context),
            ["GetAllInstances"] = async (sequence, context) => await readModels.GetAllInstances(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "read-model", EventSequenceId = sequence }, context),
            ["Watch"] = async (sequence, context) => await readModels.Watch(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "read-model", EventSequenceId = sequence }, context).FirstAsync().ToTask(),
            ["DehydrateSession"] = (sequence, context) => readModels.DehydrateSession(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "read-model", EventSequenceId = sequence }, context)
        };
    }

    async Task Because()
    {
        foreach (var request in _requests)
        {
            _errors[request.Key] = [];
            foreach (var sequence in new[] { "system", "SYSTEM", " SyStEm ", "\tSyStEm\r\n" })
            {
                await InvokeForCallers(request.Key, request.Value, sequence);
            }
        }
    }

    async Task InvokeForCallers(string method, Func<string, CallContext, Task> invoke, string sequence)
    {
        foreach (var caller in new[] { Administrator, RevokedAdministrator, OrdinaryUser, Application, Anonymous })
        {
            var context = CallFor(new Probe(), CallerNamed(caller));
            var error = await Catch.Exception(() => invoke(sequence, context));
            _errors[method].Add(error);
            if (caller == Administrator) _administratorErrors.Add(error);
        }
        _errors[method].Add(await Catch.Exception(() => invoke(sequence, default)));
    }

    bool RefusedEveryRequest(string method) =>
        _errors[method].Count == 24 && _errors[method].TrueForAll(_ => _ is RpcException { StatusCode: StatusCode.PermissionDenied });

    [Fact] void should_refuse_every_administrator_request() => (_administratorErrors.Count == 24 && _administratorErrors.TrueForAll(_ => _ is RpcException { StatusCode: StatusCode.PermissionDenied })).ShouldBeTrue();
    [Fact] void should_refuse_registration_for_every_caller_and_variant() => RefusedEveryRequest("Register").ShouldBeTrue();
    [Fact] void should_refuse_preview_for_every_caller_and_variant() => RefusedEveryRequest("Preview").ShouldBeTrue();
    [Fact] void should_refuse_instance_queries_for_every_caller_and_variant() => RefusedEveryRequest("GetInstanceByKey").ShouldBeTrue();
    [Fact] void should_refuse_all_instance_queries_for_every_caller_and_variant() => RefusedEveryRequest("GetAllInstances").ShouldBeTrue();
    [Fact] void should_refuse_watches_for_every_caller_and_variant() => RefusedEveryRequest("Watch").ShouldBeTrue();
    [Fact] void should_refuse_session_dehydration_for_every_caller_and_variant() => RefusedEveryRequest("DehydrateSession").ShouldBeTrue();
}
