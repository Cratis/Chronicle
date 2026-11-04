// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeSetCache;

public class when_inputs_differ : Specification
{
    readonly EventTypeSetCache<object> _cache = new();
    object _original;
    object _membership;
    object _artifact;
    object _model;

    void Because()
    {
        _original = Get([typeof(string)], typeof(string), typeof(int));
        _membership = Get([typeof(string), typeof(int)], typeof(string), typeof(int));
        _artifact = Get([typeof(string)], typeof(int), typeof(int));
        _model = Get([typeof(string)], typeof(string), typeof(string));
    }

    object Get(Type[] types, Type artifact, Type model) => _cache.Get(types, artifact, model, _ => new object());

    [Fact] void should_separate_event_membership() => _membership.ShouldNotBeSame(_original);
    [Fact] void should_separate_artifacts() => _artifact.ShouldNotBeSame(_original);
    [Fact] void should_separate_models() => _model.ShouldNotBeSame(_original);
}
