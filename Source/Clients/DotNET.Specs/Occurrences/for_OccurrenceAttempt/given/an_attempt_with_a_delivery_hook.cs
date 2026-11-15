// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.for_ObserverDelivery.given;
using Cratis.Chronicle.Occurrences.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.given;

public class an_attempt_with_a_delivery_hook : an_occurrence_client
{
    protected List<string> _log;

    void Establish()
    {
        _log = [];
        _services = new ServiceCollection()
            .AddSingleton<IObserverDeliveryScope>(new a_recording_delivery_scope(_log))
            .BuildServiceProvider();
        _activator.Activate(Arg.Any<IServiceProvider>(), typeof(TestOccurrenceHandler)).Returns(_ =>
        {
            _log.Add($"activated:{ObserverDeliveryContext.Current?.Namespace}");
            return new ActivatedArtifact(_handler, typeof(TestOccurrenceHandler), NullLogger<ActivatedArtifact>.Instance);
        });
    }

    private protected Task Run(OccurrenceDeliveryScope? deliveryScope) =>
        new OccurrenceAttempt(
            _store,
            _service,
            typeof(TestTrigger),
            typeof(TestOccurrenceHandler),
            _services,
            _activator,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            _causation,
            _identity,
            TimeProvider.System,
            NullLogger<Occurrences>.Instance,
            deliveryScope).Run(_claim, TimeProvider.System.GetTimestamp(), CancellationToken.None);
}
