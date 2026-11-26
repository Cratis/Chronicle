// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections;

public class when_previewing_flat_keys : Specification
{
    PreviewSink _sink;
    FlatCompositeKey _first;
    FlatCompositeKey _second;

    void Establish()
    {
        _sink = new PreviewSink("id");
        _first = Key("first");
        _second = Key("second");
        _sink.Set(FlatCompositeKeyState.Text(_first), State("first"), 1);
        _sink.Set(FlatCompositeKeyState.Text(_second), State("second"), 2);
    }

    [Fact]
    async Task should_keep_separate_tuples_without_using_expando_to_string()
    {
        FlatCompositeKeyState.Text(_first).ShouldEqual(CompositeKeyEncoding.Encode(_first.Parts));
        FlatCompositeKeyState.Text(_first).ShouldNotEqual(FlatCompositeKeyState.Text(_second));
        _sink.ReadModels.Count().ShouldEqual(2);
        ((IDictionary<string, object?>)(await _sink.FindOrDefault(_first))!)["number"].ShouldEqual("first");
        ((IDictionary<string, object?>)(await _sink.FindOrDefault(_second))!)["number"].ShouldEqual("second");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_leave_the_cached_state_untouched_when_identity_is_violated(bool removePart)
    {
        var incoming = AppendedEvent.Empty();
        var changes = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, _sink.GetInitialState(_first));
        var context = new Engine.ProjectionEventContext(_first, incoming, changes, default, false);
        var projection = Substitute.For<Engine.IProjection>();
        projection.InitialModelState.Returns(new ExpandoObject());
        projection.TargetReadModelSchema.Returns(await JsonSchema.FromJsonAsync("{\"type\":\"object\"}"));
        await new SetInitialState(_sink, NullLogger<SetInitialState>.Instance).Perform(projection, context);
        var values = (IDictionary<string, object?>)changes.CurrentState;
        if (removePart) values.Remove("customer");
        else values["customer"] = "different";
        Catch.Exception(() => FlatCompositeKeyState.Verify(changes, _first)).ShouldBeOfExactType<FlatCompositeKeyIdentityViolated>();
        _sink.TryGet(FlatCompositeKeyState.Text(_first), out var state).ShouldBeTrue();
        ((IDictionary<string, object?>)state)["customer"].ShouldEqual("customer");
        ((IDictionary<string, object?>)state).ContainsKey("id").ShouldBeFalse();
    }

    static FlatCompositeKey Key(string number) => new(State(number), ArrayIndexers.NoIndexers);

    static ExpandoObject State(string number)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["customer"] = "customer";
        ((IDictionary<string, object?>)state)["number"] = number;
        return state;
    }
}
