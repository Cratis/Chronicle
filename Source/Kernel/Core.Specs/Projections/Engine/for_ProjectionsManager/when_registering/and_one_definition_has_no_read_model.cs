// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_registering;

public class and_one_definition_has_no_read_model : given.a_projections_manager
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => _manager.Register(
        _eventStore,
        [_firstDefinition, _secondDefinition],
        [_firstReadModelDefinition],
        [_namespace]));

    [Fact] void should_fail_the_registration() => _exception.ShouldBeOfExactType<ProjectionDefinitionsRegistrationFailed>();
    [Fact] void should_attribute_the_failure_to_the_definition_that_failed() => ((ProjectionDefinitionsRegistrationFailed)_exception).Failures.Keys.ShouldContainOnly(_secondDefinition.Identifier);
    [Fact] void should_keep_the_root_cause() => _exception.GetBaseException().ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_still_register_the_definition_that_succeeded() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _).ShouldBeTrue();
    [Fact] void should_not_register_the_definition_that_failed() => _manager.TryGet(_eventStore, _namespace, _secondDefinition.Identifier, out _).ShouldBeFalse();
}
