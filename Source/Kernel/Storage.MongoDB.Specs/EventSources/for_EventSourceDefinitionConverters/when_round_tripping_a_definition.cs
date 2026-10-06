// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSources;

namespace Cratis.Chronicle.Storage.MongoDB.EventSources.for_EventSourceDefinitionConverters;

public class when_round_tripping_a_definition : Specification
{
    Concepts.EventSources.EventSourceDefinition _original;
    EventSourceDefinition _document;
    Concepts.EventSources.EventSourceDefinition _result;

    void Establish() => _original = new(
        "ShoppingCart",
        "A cart",
        EventSourceOwner.Kernel,
        ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType,
        [
            new Concepts.EventSources.EventStreamDefinition("Items", "The items", ConcurrencyDimensions.EventStreamId),
            new Concepts.EventSources.EventStreamDefinition("Payment", string.Empty, ConcurrencyDimensions.None)
        ]);

    void Because()
    {
        _document = _original.ToMongoDB();
        _result = _document.ToKernel();
    }

    [Fact] void should_use_the_name_as_the_document_id() => _document.Id.ShouldEqual("ShoppingCart");
    [Fact] void should_keep_the_name() => _result.Name.ShouldEqual(_original.Name);
    [Fact] void should_keep_the_description() => _result.Description.ShouldEqual(_original.Description);
    [Fact] void should_keep_the_owner() => _result.Owner.ShouldEqual(EventSourceOwner.Kernel);
    [Fact] void should_keep_the_concurrency() => _result.Concurrency.ShouldEqual(_original.Concurrency);
    [Fact] void should_keep_the_streams_in_order() => _result.Streams.Select(_ => _.Name.Value).ShouldEqual(["Items", "Payment"]);
    [Fact] void should_keep_the_stream_concurrency() => _result.Streams.First().Concurrency.ShouldEqual(ConcurrencyDimensions.EventStreamId);
}
