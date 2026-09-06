// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_registering;

public class and_one_definition_has_no_read_model : given.a_projections_manager
{
    ProjectionRegistrationError _error;

    async Task Because()
    {
        var result = await _manager.Register(
            _eventStore,
            [_firstDefinition, _secondDefinition],
            [_firstReadModelDefinition],
            [_namespace]);
        result.TryGetError(out _error);
    }

    [Fact] void should_report_a_registration_error() => _error.ShouldNotBeNull();
    [Fact] void should_attribute_the_failure_to_the_definition_that_failed() => _error.Failures.Keys.ShouldContainOnly(_secondDefinition.Identifier);
    [Fact] void should_keep_the_root_cause() => _error.Failures[_secondDefinition.Identifier].GetBaseException().ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_still_register_the_definition_that_succeeded() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _).ShouldBeTrue();
    [Fact] void should_not_register_the_definition_that_failed() => _manager.TryGet(_eventStore, _namespace, _secondDefinition.Identifier, out _).ShouldBeFalse();
}
