// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Federation.given;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalRecovery.given;

public class a_removal : Specification
{
    protected static readonly EventStoreNamespaceName _name = "tenant";
    protected INamespaceLifecycleStorage _storage;
    protected NamespaceLifecycle _state;
    protected TimerClock _clock;
    protected NamespaceRemovalRecovery _recovery;
    protected readonly List<NamespaceRemovalStage> _effects = [];
    protected readonly List<NamespaceLifecycle> _issued = [];
    protected Func<NamespaceLifecycle, Task> _execute;

    void Establish()
    {
        _clock = new();
        _state = NamespaceLifecycle.Open(_name, NamespaceIncarnationId.New(), _clock.GetUtcNow()).BeginRemoval(Guid.NewGuid(), _clock.GetUtcNow());
        _storage = Substitute.For<INamespaceLifecycleStorage>();
        _storage.Get(_name).Returns(_ => Task.FromResult<NamespaceLifecycle?>(_state));
        _storage.TryWrite(_name, Arg.Any<long?>(), Arg.Any<NamespaceLifecycle>()).Returns(call =>
        {
            if (call.ArgAt<long?>(1) != _state.Revision) return Task.FromResult(false);
            _state = call.ArgAt<NamespaceLifecycle>(2);
            return Task.FromResult(true);
        });
        _execute = _ => Task.CompletedTask;
        _recovery = CreateRecovery();
    }

    protected NamespaceRemovalRecovery CreateRecovery() => new(
        _storage,
        state =>
        {
            _effects.Add(state.Stage);
            _issued.Add(_state);
            return _execute(state);
        },
        _clock);
}
