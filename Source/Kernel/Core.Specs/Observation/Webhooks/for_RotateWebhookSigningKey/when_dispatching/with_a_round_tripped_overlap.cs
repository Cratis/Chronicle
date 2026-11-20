// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf;
using RotateWebhookSigningKeyRequest = Cratis.Chronicle.Contracts.Observation.Webhooks.RotateWebhookSigningKeyRequest;
using WebhooksService = Cratis.Chronicle.Services.Observation.Webhooks.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKey.when_dispatching;

public class with_a_round_tripped_overlap : Signing.for_WebhookSigningKeyRing.given.a_signing_key_ring
{
    [Theory]
    [InlineData(false, 0, 86400)]
    [InlineData(false, 120, 86400)]
    [InlineData(true, 0, 0)]
    [InlineData(true, 120, 120)]
    public async Task should_preserve_the_overlap_choice_through_codec_and_dispatch(bool useSpecifiedOverlap, double seconds, double expectedSeconds)
    {
        await CreateRing();
        var originalId = (await _ring.GetSigningKeys()).Single().KeyId;
        var request = Serializer.DeepClone(new RotateWebhookSigningKeyRequest
        {
            EventStore = "the-store",
            UseSpecifiedOverlap = useSpecifiedOverlap,
            OverlapSeconds = seconds
        });
        await using var scenario = ChronicleCommandScenario.For<RotateWebhookSigningKey>();
        scenario.Services.AddChronicleAdministratorAuthorization();
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        scenario.Services.AddSingleton(storage);
        var factory = Substitute.For<IGrainFactory>();
        factory.GetGrain<IWebhookSigningKeyRing>("the-store").Returns(_ring);
        scenario.Services.AddSingleton(factory);

        // The generated service's dispatch runs the decoded command through the real Arc scenario pipeline.
        var dispatch = Substitute.For<ICommandPipeline>();
        dispatch.Execute(Arg.Any<object>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(call => scenario.Execute((RotateWebhookSigningKey)call.Arg<object>()));
        var service = new WebhooksService(dispatch, factory, storage, NullLogger<WebhooksService>.Instance);
        var result = await service.RotateWebhookSigningKey(request);

        result.IsSuccess.ShouldBeTrue();
        var previous = State.Keys.Single(key => key.KeyId == originalId);
        previous.Status.ShouldEqual(expectedSeconds == 0 ? WebhookSigningKeyStatus.Retired : WebhookSigningKeyStatus.Overlap);
        previous.OverlapEndsAt.ShouldEqual(Now.AddSeconds(expectedSeconds));
    }
}
