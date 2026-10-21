// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers.for_ReducerFingerprint.given;

namespace Cratis.Chronicle.Reducers.for_SelfReducerFingerprint.when_the_inherited_body_changes;

public class and_only_the_async_hook_is_edited : Specification
{
    string _original;
    string _recompiled;
    string _changed;
    string _fallback;
    string _fallbackAlias;

    void Because()
    {
        _original = Fingerprint(1);
        _recompiled = Fingerprint(1);
        _changed = Fingerprint(2);
        _fallback = Fingerprint(1, fallback: true);
        _fallbackAlias = Fingerprint(1, "changed-alias", fallback: true);
    }

    [Fact] void should_ignore_hook_recompilation_noise() => _recompiled.ShouldEqual(_original);
    [Fact] void should_hash_the_inherited_async_hook_state_machine() => _changed.ShouldNotEqual(_original);
    [Fact] void should_include_dispatch_in_the_conservative_fallback() => _fallbackAlias.ShouldNotEqual(_fallback);
    [Fact] void should_use_a_distinct_conservative_fingerprint() => _fallback.ShouldNotEqual(_original);

    static string Fingerprint(int adjustment, string id = "event", bool fallback = false)
    {
        var source = $$"""
            #nullable enable
            namespace HookFingerprint;
            public record Event(int Value);
            public record Base
            {
                public static async System.Threading.Tasks.Task BeginReplay()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                    _ = new Model({{adjustment}});
                }
            }
            public record Model(int Value) : Base
            {
                public static Model Reduce(Event @event, Model? current) => new(@event.Value);
            }
            """;
        var assembly = Assembly.Load(CompiledReducer.CompileAssembly("HookFingerprint", source));
        var model = assembly.GetType("HookFingerprint.Model")!;
        var @event = assembly.GetType("HookFingerprint.Event")!;
        var registry = RegisteredEventTypesForSpecifications.Create((@event, new EventType(id, 1)));
        return fallback ? SelfReducerFingerprint.CreateFallback(registry, model) : SelfReducerFingerprint.Create(registry, model);
    }
}
