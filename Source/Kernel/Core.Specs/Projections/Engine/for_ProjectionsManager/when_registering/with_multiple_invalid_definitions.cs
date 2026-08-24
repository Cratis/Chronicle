// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_registering;

public class with_multiple_invalid_definitions : given.a_projections_manager
{
    ProjectionRegistrationError _error;

    async Task Because()
    {
        var result = await _manager.Register(
            _eventStore,
            [_firstDefinition, _secondDefinition],
            [],
            [_namespace]);
        result.TryGetError(out _error);
    }

    [Fact] void should_report_a_registration_error() => _error.ShouldNotBeNull();
    [Fact] void should_report_every_failed_definition() => _error.Failures.Keys.ShouldContainOnly(_firstDefinition.Identifier, _secondDefinition.Identifier);
    [Fact] void should_not_register_the_first_definition() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _).ShouldBeFalse();
    [Fact] void should_not_register_the_second_definition() => _manager.TryGet(_eventStore, _namespace, _secondDefinition.Identifier, out _).ShouldBeFalse();
}
