// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_from_context;

public class with_invalid_property : given.a_model_bound_projection_builder
{
    Exception? _result;

    void Because() => _result = Catch.Exception(() => builder.Build(typeof(EventAuditEntry)));

    [Fact] void should_throw_invalid_property_for_type() => _result.ShouldBeOfExactType<InvalidPropertyForType>();
    [Fact] void should_include_event_context_type_in_message() => _result?.Message.ShouldContain(typeof(EventContext).FullName!);
    [Fact] void should_include_property_name_in_message() => _result?.Message.ShouldContain("InvalidProperty");

    record EventAuditEntry(
        [Key]
        Guid Id,

        [SetFromContext<DebitAccountOpened>("InvalidProperty")]
        DateTimeOffset Timestamp);
}
