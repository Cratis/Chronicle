// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.ExternalServices;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ExternalServices.for_ExternalService;

public class when_querying_over_grpc : given.stored_headers
{
    readonly List<ExternalServiceResponse> _responses = [];
    readonly List<string> _wirePayloads = [];

    async Task Because()
    {
        var service = new Services.ExternalServices.ExternalServices(Substitute.For<ICommandPipeline>(), _storage,
            Substitute.For<ILogger<Services.ExternalServices.ExternalServices>>());
        var listed = await service.GetExternalServices(new() { EventStore = "store" });
        RoundTrip(listed.Data.Single());
        using var subscription = service.ObserveExternalServices(new() { EventStore = "store" })
            .Subscribe(result => RoundTrip(result.Data.Single()));
        _source.OnNext([_definition]);
        _headers["x-empty"] = "synthetic-new";
        _source.OnNext([_definition]);
    }

    void RoundTrip(ExternalServiceResponse response)
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, response);
        _wirePayloads.Add(Encoding.UTF8.GetString(stream.ToArray()));
        stream.Position = 0;
        _responses.Add(ProtoBuf.Serializer.Deserialize<ExternalServiceResponse>(stream));
    }

    [Fact] void should_return_a_populated_list_and_two_observations() => _responses.Count.ShouldEqual(3);
    [Fact] void should_keep_all_names_on_every_response() => _responses.All(_ => _.Headers.Count == 4).ShouldBeTrue();
    [Fact] void should_return_no_legacy_values() => _responses.SelectMany(_ => _.Headers.Values).All(_ => _ == string.Empty).ShouldBeTrue();
    [Fact] void should_keep_true_metadata() => _responses.All(_ => _.HeaderValuesSet["x-first"]).ShouldBeTrue();
    [Fact] void should_keep_false_metadata() => _responses[0].HeaderValuesSet["x-empty"].ShouldBeFalse();
    [Fact] void should_update_metadata_on_the_stream() => _responses[2].HeaderValuesSet["x-empty"].ShouldBeTrue();
    [Fact] void should_never_put_values_on_the_wire() => _wirePayloads.All(_ => !_.Contains("synthetic-", StringComparison.Ordinal)).ShouldBeTrue();
}
