// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_Projections.given;

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class and_the_projection_removes_its_root_via_join : projections_for_explicit_registration
{
    a_logger_that_remembers_warnings _logger;
    Projections _projectionsWithLogger;

    void Establish()
    {
        _logger = new();
        _lifecycle.IsConnected.Returns(true);
        _projectionsWithLogger = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            _logger);
    }

    async Task Because()
    {
        await _projectionsWithLogger.Register<Inventory>(projection => projection.RemovedWithJoin<ItemRemoved>(), "inventory");
        await _projectionsWithLogger.Register<Customer>(projection => projection.From<CustomerRegistered>(), "customers");
    }

    [Fact] void should_warn_once() => _logger.Warnings.Count.ShouldEqual(1);
    [Fact] void should_name_the_read_model() => _logger.Warnings[0].ShouldContain(nameof(Inventory));
    [Fact] void should_still_register_the_projection() => _projectionsWithLogger.HasFor<Inventory>().ShouldBeTrue();
}
