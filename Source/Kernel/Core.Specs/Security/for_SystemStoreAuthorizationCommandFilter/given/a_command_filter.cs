// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Security.for_SystemStoreAuthorizationCommandFilter.given;

public class a_command_filter : Security.given.a_system_store_caller
{
    protected internal record ApplicationCommand(string EventStore, string Namespace);

    protected SystemStoreAuthorizationCommandFilter _filter;
    protected CommandResult? _result;

    protected CommandContext ContextFor(object command) => new(
        CorrelationId.New(), command.GetType(), command, [], new CommandContextValues(), ServiceProvider: CreateServices());

    protected static RetryPartition Retry(string eventStore = "Store", string @namespace = "Default", string observerId = "observer", string sequence = "event-log") =>
        new(eventStore, @namespace, observerId, sequence, "partition");

    void Establish() => _filter = new SystemStoreAuthorizationCommandFilter();
}
