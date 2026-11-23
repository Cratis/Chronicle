// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Federation.given;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator.given;

public class a_coordinator : Specification
{
    protected static readonly EventStoreNamespaceName _name = "tenant";
    protected INamespaceLifecycleStorage _lifecycle;
    protected INamespaceMarkerStorage _marker;
    protected INamespaceIncarnationEffects _effects;
    protected NamespaceLifecycle _state;
    protected NamespaceLifecycle _result;
    protected NamespaceMarker _markerState;
    protected TimerClock _clock;
    protected NamespaceIncarnationCoordinator _coordinator;
    protected readonly List<string> _order = [];
    protected readonly List<NamespaceLifecycle> _writes = [];

    void Establish()
    {
        _clock = new();
        _lifecycle = Substitute.For<INamespaceLifecycleStorage>();
        _lifecycle.Get(_name).Returns(_ => Task.FromResult<NamespaceLifecycle?>(_state));
        _lifecycle.TryWrite(_name, Arg.Any<long?>(), Arg.Any<NamespaceLifecycle>()).Returns(call =>
        {
            if (call.ArgAt<long?>(1) != _state?.Revision) return Task.FromResult(false);
            _state = call.ArgAt<NamespaceLifecycle>(2);
            _writes.Add(_state);
            _order.Add($"Write{_state.Status}");
            return Task.FromResult(true);
        });
        _marker = Substitute.For<INamespaceMarkerStorage>();
        _marker.Get().Returns(_ => Task.FromResult<NamespaceMarker?>(_markerState));
        _marker.TryOpen(Arg.Any<NamespaceIncarnationId>(), Arg.Any<NamespaceIncarnationId?>()).Returns(call =>
        {
            _order.Add("OpenMarker");
            _markerState = new(call.ArgAt<NamespaceIncarnationId>(0), true, 0);
            return Task.FromResult(true);
        });
        _marker.TryClose(Arg.Any<NamespaceIncarnationId>()).Returns(call =>
        {
            _order.Add("CloseMarker");
            _markerState = new(call.Arg<NamespaceIncarnationId>(), false, 0);
            return Task.FromResult(true);
        });
        _effects = Substitute.For<INamespaceIncarnationEffects>();
        _effects.EnsureWake(Arg.Any<long>()).Returns(_ => Record("Wake"));
        _effects.Create(Arg.Any<NamespaceLifecycle>()).Returns(_ => Record("Create"));
        _effects.Remove(Arg.Any<NamespaceLifecycle>()).Returns(call => Record(call.Arg<NamespaceLifecycle>().Stage.ToString()));
        _coordinator = CreateCoordinator();
    }

    protected NamespaceIncarnationCoordinator CreateCoordinator() => new(_name, _lifecycle, _marker, _effects, _clock);

    Task Record(string effect)
    {
        _order.Add(effect);
        return Task.CompletedTask;
    }
}
