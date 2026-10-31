// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_using_explicit_artifacts;

public class and_a_plain_reactor_is_nominated : Specification
{
    ReactorScenario<Notifier> _scenario;

    void Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<Requested>().RegisterEventType<Notified>().RegisterReactor<Notifier>("plain-notifier");
        _scenario = new(new Defaults(new InMemoryClientArtifactsProvider(), null, options));
    }

    Task Because() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new Requested("explicit"));
    void Destroy() => _scenario.Dispose();

    [Fact] void should_invoke_and_record_the_plain_reactors_output() => _scenario.ShouldHaveProduced<Notified>(_ => _.Message == "explicit");

    public record Requested(string Message);
    public record Notified(string Message);
    public class Notifier
    {
        public Notified Handle(Requested @event) => new(@event.Message);
    }
}
