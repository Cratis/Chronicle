// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_callback_on_an_annotated_model : given.projections_with_competing_ids
{
    IProjectionHandler _default;
    IProjectionHandler _override;

    async Task Because()
    {
        _default = await _projections.Register<FirstModel>(_ => { });
        await _projections.Register<FirstModel>(_ => { });
        _override = await _projections.Register<SecondModel>(_ => { }, "callback-id");
    }

    [Fact] void should_use_the_attribute_when_no_callback_id_is_supplied() => _default.Id.ShouldEqual((ProjectionId)SharedId);
    [Fact] void should_preserve_the_supplied_callback_id() => _override.Id.ShouldEqual((ProjectionId)"callback-id");
    [Fact] void should_allow_replacing_the_same_owner_with_the_same_id() => _projections.GetAllHandlers().Count().ShouldEqual(2);
    [Fact] void should_publish_each_effective_definition_once() => _projections.Definitions.Count.ShouldEqual(2);
    [Fact] void should_resolve_the_callback_override_by_model() => _projections.GetProjectionIdForModel<SecondModel>().ShouldEqual((ProjectionId)"callback-id");
}
