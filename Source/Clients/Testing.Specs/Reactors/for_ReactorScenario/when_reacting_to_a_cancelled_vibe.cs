// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_reacting_to_a_cancelled_vibe : Specification
{
    const string Host = "Ada";

    ReactorScenario<VibeCancellationReactor> _scenario;
    INotificationService _notifications;
    IVibeAudit _audit;
    EventSourceId _vibeId;

    void Establish()
    {
        _vibeId = EventSourceId.New();
        _notifications = Substitute.For<INotificationService>();
        _audit = Substitute.For<IVibeAudit>();

        _scenario = new ReactorScenario<VibeCancellationReactor>();
        _scenario.Services.AddSingleton(_notifications);
        _scenario.Services.AddSingleton(_audit);
        _scenario.Given.ForEventSourceId(_vibeId).ReadModel(new VibeAttendees(_vibeId, Host));
    }

    async Task Because() => await _scenario.Given.ForEventSource(_vibeId).Events(new VibeCancelled());

    [Fact] async Task should_notify_the_host_from_the_seeded_read_model() => await _notifications.Received(1).Notify(Host);
    [Fact] void should_audit_through_the_method_parameter_service() => _audit.Received(1).Record(Host);
}
