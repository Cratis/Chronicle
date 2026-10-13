// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifacts;

public class when_a_captured_builder_is_modified_later : Specification
{
    ExplicitArtifacts _options;
    IEventTypeRegistrationBuilder _builder;

    void Establish()
    {
        _options = new();
        _options.RegisterEventType<Event>(_ => _builder = _);
    }

    void Because() => _builder.WithId("too-late").Generation(2).Public();

    [Fact] void should_not_retain_mutable_builder_state() => _options.CreateSnapshot().EventTypes.Single().Id.ShouldBeNull();
    [Fact] void should_not_change_captured_generation() => _options.CreateSnapshot().EventTypes.Single().Generation.ShouldBeNull();
    [Fact] void should_not_change_captured_visibility() => _options.CreateSnapshot().EventTypes.Single().Visibility.ShouldBeNull();

    public record Event;
}
