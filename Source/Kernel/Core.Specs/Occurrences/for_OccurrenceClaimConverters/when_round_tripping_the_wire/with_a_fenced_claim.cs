// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;
using ProtoBuf;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceClaimConverters.when_round_tripping_the_wire;

public class with_a_fenced_claim : Specification
{
    Contracts.Occurrences.ClaimOccurrencesResponse _response;
    void Because()
    {
        var occurrence = new Occurrence(new("occurrence"), new("trigger"), OccurrenceKind.Application, "fingerprint", "{\"value\":1}", 4, DateTimeOffset.UnixEpoch, null, null, OccurrenceState.Leased, 2, 7, "worker", DateTimeOffset.UnixEpoch.AddMinutes(1), null, null);
        var claim = OccurrenceClaimConverters.FromJson(JsonSerializer.Serialize(new[] { occurrence })).Single().ToContract();
        _response = Serializer.DeepClone(new Contracts.Occurrences.ClaimOccurrencesResponse { Claims = [claim] });
    }

    [Fact] void should_preserve_the_payload() => _response.Claims.Single().Payload.ShouldEqual("{\"value\":1}");
    [Fact] void should_preserve_the_fencing_epoch() => _response.Claims.Single().Epoch.ShouldEqual(7);
    [Fact] void should_preserve_the_schema_revision() => _response.Claims.Single().SchemaRevision.ShouldEqual(4);
    [Fact] void should_preserve_the_kernel_expiry() => ((DateTimeOffset)_response.Claims.Single().LeaseExpiry).ShouldEqual(DateTimeOffset.UnixEpoch.AddMinutes(1));
}
