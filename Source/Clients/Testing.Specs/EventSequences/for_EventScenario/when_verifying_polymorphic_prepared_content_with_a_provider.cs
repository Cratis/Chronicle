// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_verifying_polymorphic_prepared_content_with_a_provider : Specification
{
    public interface IShape;

    [DerivedType("verification-circle", typeof(IShape))]
    public record Circle(int Radius) : IShape;

    [EventType]
    public record ShapeRecorded(IShape Shape, string Enrichment);

    EventScenario _scenario;
    ServiceProvider _services;
    ICanProvideAdditionalEventInformation _provider;
    PreparedEvent _prepared;
    AppendResult _append;
    ContentVerificationResult _result;
    ContentVerificationResult _different;

    async Task Establish()
    {
        _provider = Substitute.For<ICanProvideAdditionalEventInformation>();
        _provider.ProvideFor(Arg.Any<JsonObject>()).Returns(call =>
        {
            var content = call.Arg<JsonObject>();
            content["enrichment"] = "provided";
            content["undeclared"] = "not stored";
            return Task.CompletedTask;
        });
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(ShapeRecorded)]);
        artifacts.AdditionalEventInformationProviders.Returns([typeof(ICanProvideAdditionalEventInformation)]);
        _services = new ServiceCollection().AddSingleton(_provider).BuildServiceProvider();
        _scenario = new(new Defaults(artifacts, _services));
        _prepared = await _scenario.EventLog.Prepare(new ShapeRecorded(new Circle(5), "original"));
        _append = await _scenario.EventLog.AppendPrepared("source", _prepared);
        _append.ShouldBeSuccessful();
    }

    async Task Because()
    {
        _result = await _scenario.EventLog.VerifyContent(_append.SequenceNumber, _prepared, "source");
        var changed = await _scenario.EventLog.Prepare(new ShapeRecorded(new Circle(6), "original"));
        _different = await _scenario.EventLog.VerifyContent(_append.SequenceNumber, changed, "source");
    }

    [Fact] void should_compare_the_stored_polymorphic_and_provider_content() => _result.ShouldEqual(ContentVerificationResult.Equal);
    [Fact] void should_detect_a_changed_derived_property() => _different.ShouldEqual(ContentVerificationResult.Different);
    [Fact] void should_run_the_provider_only_for_each_preparation() => _provider.Received(2).ProvideFor(Arg.Any<JsonObject>());

    async Task Destroy()
    {
        _scenario.Dispose();
        await _services.DisposeAsync();
    }
}
