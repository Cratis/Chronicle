// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnation.given;

public class an_owner_with_an_unknown_control_write : an_owner
{
    protected NamespaceLifecycle _delayed;
    protected Exception _error;
    protected string[] _pendingWakes;
    protected string[] _terminalWakes;

    protected void LoseNextControlReply()
    {
        _lifecycle.TryWrite(Arg.Any<EventStoreNamespaceName>(), Arg.Any<long?>(), Arg.Any<NamespaceLifecycle>()).Returns(call =>
        {
            if (call.ArgAt<long?>(1) != _state?.Revision) return Task.FromResult(false);
            var next = call.ArgAt<NamespaceLifecycle>(2);
            if (_delayed is null)
            {
                _delayed = next;
                return Task.FromException<bool>(new TimeoutException("Control acknowledgement lost"));
            }
            _state = next;
            _writes.Add(next);
            return Task.FromResult(true);
        });
    }

    protected async Task DeliverDelayedWriteAndWake()
    {
        await _owner.Wake();
        _pendingWakes = (await _owner.GetReminders()).Select(_ => _.ReminderName).ToArray();
        _state = _delayed;
        await _owner.ReceiveReminder(_pendingWakes.Single(), default);
        _terminalWakes = (await _owner.GetReminders()).Select(_ => _.ReminderName).ToArray();
    }
}
