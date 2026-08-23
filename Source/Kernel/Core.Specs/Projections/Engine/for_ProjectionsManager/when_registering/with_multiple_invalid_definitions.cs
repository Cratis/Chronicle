// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_registering;

public class with_multiple_invalid_definitions : given.a_projections_manager
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => _manager.Register(
        _eventStore,
        [_firstDefinition, _secondDefinition],
        [],
        [_namespace]));

    [Fact] void should_fail_the_registration() => _exception.ShouldBeOfExactType<ProjectionDefinitionsRegistrationFailed>();
    [Fact] void should_report_every_failed_definition() => ((ProjectionDefinitionsRegistrationFailed)_exception).Failures.Keys.ShouldContainOnly(_firstDefinition.Identifier, _secondDefinition.Identifier);
    [Fact] void should_not_register_the_first_definition() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _).ShouldBeFalse();
    [Fact] void should_not_register_the_second_definition() => _manager.TryGet(_eventStore, _namespace, _secondDefinition.Identifier, out _).ShouldBeFalse();
}
