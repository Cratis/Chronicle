// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_competing_owner_after_discovery : given.projections_with_competing_ids
{
    IProjectionHandler _first;
    Exception _error;

    async Task Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(FirstFluent)]);
        await _projections.Discover();
        _first = _projections.GetHandlerFor<FirstFluent>();
        _lifecycle.IsConnected.Returns(true);
    }

    async Task Because() => _error = await Catch.Exception(() => _projections.Register<SecondModel>(_ => { }, SharedId));

    [Fact] void should_reject_the_callback_owner() => _error.ShouldBeOfExactType<MultipleProjectionsWithSameIdFound>();
    [Fact] void should_identify_the_fluent_owner() => _error.Message.ShouldContain(typeof(FirstFluent).FullName);
    [Fact] void should_identify_the_callback_owner() => _error.Message.ShouldContain(typeof(SecondModel).FullName);
    [Fact] void should_keep_the_existing_handler() => _projections.GetAllHandlers().Single().ShouldEqual(_first);
    [Fact] void should_not_publish_the_callback_handler() => _projections.HasFor<SecondModel>().ShouldBeFalse();
    [Fact] void should_keep_the_existing_definition() => _projections.Definitions.Single().Identifier.ShouldEqual(SharedId);
    [Fact] void should_keep_the_existing_artifact() => _projections.ArtifactRegistrations.Single().ArtifactType.ShouldEqual(typeof(FirstFluent));
    [Fact] void should_not_register_remotely() => _registrations.ShouldBeEmpty();
    [Fact] void should_not_register_the_read_model_remotely() => _readModels.DidNotReceive().Register<SecondModel>();
}
