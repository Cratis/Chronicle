// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_SkippedReducerMethods;

public class when_describing_skipped_methods : Specification
{
    string _description;

    void Because() => _description = SkippedReducerMethods.Describe(typeof(SkippedReducer), typeof(Model), [typeof(KnownEvent)]);

    [Fact] void should_explain_a_read_model_return_without_current_state() => _description.ShouldContain("ReturnModel");
    [Fact] void should_explain_a_known_event_parameter_without_current_state() => _description.ShouldContain("MissingCurrent");
    [Fact] void should_explain_a_by_reference_read_model_parameter() => _description.ShouldContain("ByReference");
    [Fact] void should_describe_the_missing_current_state() => _description.ShouldContain("method must take two or three parameters");
    [Fact] void should_describe_the_by_reference_parameter() => _description.ShouldContain("parameters must not be passed by reference");

    public record Model;
    [EventType] public record KnownEvent;
    public class SkippedReducer : IReducerFor<Model>
    {
        public Model ReturnModel(string id) => new();
        public Task MissingCurrent(KnownEvent @event) => Task.CompletedTask;
        public Task ByReference(KnownEvent @event, ref Model current) => Task.CompletedTask;
    }
}
