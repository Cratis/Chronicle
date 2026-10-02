// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Cratis.Serialization;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_preparing_polymorphic_content_with_additional_information : given.all_dependencies
{
    public interface IShape;

    [DerivedType("prepared-circle")]
    public record Circle(int Radius) : IShape;

    public record ShapeRecorded(IShape Shape, string Enrichment);

    EventSequence _sequence;
    ICanProvideAdditionalEventInformation _provider;
    Contracts.Sequences.AppendRequest _append;
    Contracts.Sequences.VerifyContentRequest _verification;

    void Establish()
    {
        _provider = Substitute.For<ICanProvideAdditionalEventInformation>();
        _provider.ProvideFor(Arg.Any<JsonObject>()).Returns(call =>
        {
            call.Arg<JsonObject>()["Enrichment"] = "provided";
            return Task.CompletedTask;
        });
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.AdditionalEventInformationProviders.Returns([typeof(ICanProvideAdditionalEventInformation)]);
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.ActivateNonDisposable<ICanProvideAdditionalEventInformation>(typeof(ICanProvideAdditionalEventInformation)).Returns(Cratis.Monads.Catch<ICanProvideAdditionalEventInformation>.Success(_provider));
        var serializer = new EventSerializer(artifacts, activator, _eventTypes, new JsonSerializerOptions());
        _eventTypes.HasFor(typeof(ShapeRecorded)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(ShapeRecorded)).Returns(new EventType("shape", EventTypeGeneration.First));
        _identityProvider.GetCurrent().Returns(new Identity("caller", "Caller", "caller", null));
        _sequence = new("store", "tenant", "log", _connection, _eventTypes, _constraints, serializer, _correlationIdAccessor, _concurrencyScopeStrategies, _causationManager, _unitOfWorkManager, _identityProvider, new JsonSerializerOptions());
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _append = call.Arg<Contracts.Sequences.AppendRequest>();
            return CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.Empty, new() { SequenceNumber = 42 });
        });
        _sequences.VerifyContent(Arg.Any<Contracts.Sequences.VerifyContentRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _verification = call.Arg<Contracts.Sequences.VerifyContentRequest>();
            return CommandResult<Contracts.Sequences.VerifyContentResponse>.Success(Guid.Empty, new() { Result = Contracts.Sequences.ContentVerificationResult.Equal });
        });
    }

    async Task Because()
    {
        var prepared = await _sequence.Prepare(new ShapeRecorded(new Circle(5), "original"));
        await _sequence.AppendPrepared("source", prepared);
        await _sequence.VerifyContent(42UL, prepared);
    }

    [Fact] void should_run_the_provider_once() => _provider.Received(1).ProvideFor(Arg.Any<JsonObject>());
    [Fact] void should_preserve_the_provider_overwrite() => JsonNode.Parse(_append.Content)!["Enrichment"]!.GetValue<string>().ShouldEqual("provided");
    [Fact] void should_preserve_the_derived_type() => _append.Content.Contains("prepared-circle").ShouldBeTrue();
    [Fact] void should_preserve_the_subtype_property() => _append.Content.Contains("\"radius\":5").ShouldBeTrue();
    [Fact] void should_verify_the_same_snapshot() => _verification.Content.ShouldEqual(_append.Content);
}
