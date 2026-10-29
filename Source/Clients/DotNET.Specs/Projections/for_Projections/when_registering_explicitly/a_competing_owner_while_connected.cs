// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_competing_owner_while_connected : given.projections_with_competing_ids
{
    IProjectionHandler _first;
    Exception _error;

    async Task Establish()
    {
        _lifecycle.IsConnected.Returns(true);
        _first = await _projections.Register<FirstModel>();
    }

    async Task Because() => _error = await Catch.Exception(() => _projections.Register<SecondModel>());

    [Fact] void should_reject_the_second_owner() => _error.ShouldBeOfExactType<MultipleProjectionsWithSameIdFound>();
    [Fact] void should_identify_the_id() => _error.Message.ShouldContain(SharedId);
    [Fact] void should_identify_the_first_owner() => _error.Message.ShouldContain(typeof(FirstModel).FullName);
    [Fact] void should_identify_the_second_owner() => _error.Message.ShouldContain(typeof(SecondModel).FullName);
    [Fact] void should_keep_the_first_handler() => _projections.GetAllHandlers().Single().ShouldEqual(_first);
    [Fact] void should_not_publish_the_second_handler() => _projections.HasFor<SecondModel>().ShouldBeFalse();
    [Fact] void should_keep_the_first_definition() => _projections.Definitions.Single().Identifier.ShouldEqual(SharedId);
    [Fact] void should_only_register_the_first_definition_remotely() => _registrations.Count.ShouldEqual(1);
    [Fact] void should_not_register_the_second_read_model_remotely() => _readModels.DidNotReceive().Register<SecondModel>();
}
