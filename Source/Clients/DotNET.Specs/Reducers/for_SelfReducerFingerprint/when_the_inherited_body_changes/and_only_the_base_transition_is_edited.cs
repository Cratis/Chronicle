// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers.for_ReducerFingerprint.given;

namespace Cratis.Chronicle.Reducers.for_SelfReducerFingerprint.when_the_inherited_body_changes;

public class and_only_the_base_transition_is_edited : Specification
{
    string _first;
    string _same;
    string _changed;
    string _changedAlias;
    string _asyncFirst;
    string _asyncChanged;

    void Because()
    {
        _first = Fingerprint(1);
        _same = Fingerprint(1);
        _changed = Fingerprint(2);
        _changedAlias = Fingerprint(1, "other-event");
        _asyncFirst = Fingerprint(1, asynchronous: true);
        _asyncChanged = Fingerprint(2, asynchronous: true);
    }

    [Fact] void should_ignore_recompilation_noise() => _same.ShouldEqual(_first);
    [Fact] void should_detect_an_inherited_body_only_change() => _changed.ShouldNotEqual(_first);
    [Fact] void should_keep_registry_identities_in_the_fingerprint() => _changedAlias.ShouldNotEqual(_first);
    [Fact] void should_detect_an_inherited_async_state_machine_body_change() => _asyncChanged.ShouldNotEqual(_asyncFirst);

    static string Fingerprint(int adjustment, string id = "event", bool asynchronous = false)
    {
        var transition = asynchronous
            ? $"public static async System.Threading.Tasks.Task<Model> Reduce(Event @event, Model? current) {{ await System.Threading.Tasks.Task.CompletedTask; return new((current?.Value ?? 0) + @event.Value + {adjustment}); }}"
            : $"public static Model Reduce(Event @event, Model? current) => new((current?.Value ?? 0) + @event.Value + {adjustment});";
        var source = $$"""
            #nullable enable
            namespace SelfFingerprint;
            public record Event(int Value);
            public record Base
            {
                {{transition}}
            }
            public record Model(int Value) : Base;
            """;
        var image = CompiledReducer.CompileAssembly("SelfFingerprint", source);
        var assembly = Assembly.Load(image);
        var model = assembly.GetType("SelfFingerprint.Model")!;
        var @event = assembly.GetType("SelfFingerprint.Event")!;
        var registry = Substitute.For<IEventTypes>();
        registry.AllClrTypes.Returns(ImmutableList.Create(@event));
        registry.GetEventTypeFor(@event).Returns(new EventType(id, 1));
        return SelfReducerFingerprint.Create(registry, model);
    }
}
