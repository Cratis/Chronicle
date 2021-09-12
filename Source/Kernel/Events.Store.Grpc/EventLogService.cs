// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.Grpc.Contracts;
using Cratis.Execution;
using ProtoBuf.Grpc;

namespace Cratis.Events.Store.Grpc
{
    public class EventLogService : IEventLogService
    {
        readonly IEventStore _eventStore;
        readonly IExecutionContextManager _executionContextManager;

        public EventLogService(IEventStore eventStore, IExecutionContextManager executionContextManager)
        {
            _eventStore = eventStore;
            _executionContextManager = executionContextManager;
        }

        public async ValueTask<CommitResult> Commit(CommitRequest request, CallContext callContext = default)
        {
            _executionContextManager.Establish(
                Guid.Parse("f455c031-630e-450d-a75b-ca050c441708"),
                Guid.NewGuid().ToString()
            );

            await _eventStore.GetEventLog(request.EventLogId).Commit(
                request.EventSourceId,
                new EventType(request.EventType.EventTypeId, request.EventType.Generation),
                request.Content
            );

            return new CommitResult { Success = true };
        }
    }
}
