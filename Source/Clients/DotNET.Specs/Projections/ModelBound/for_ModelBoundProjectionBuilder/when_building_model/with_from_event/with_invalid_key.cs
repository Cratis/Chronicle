// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_from_event;

public class with_invalid_key : given.a_model_bound_projection_builder
{
    Exception _exception;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(UserRegisteredWithCustomId)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _exception = Catch.Exception(() => builder.Build(typeof(UserProfileWithInvalidKey)));

    [Fact] void should_throw_invalid_property_for_type() => _exception.ShouldBeOfExactType<InvalidPropertyForType>();

    [Fact]
    void should_indicate_property_name_in_exception_message()
    {
        _exception.Message.ShouldContain("NonExistentProperty");
    }

    [Fact]
    void should_indicate_event_type_in_exception_message()
    {
        _exception.Message.ShouldContain(nameof(UserRegisteredWithCustomId));
    }
}

#pragma warning disable SA1402 // File may only contain a single type
[FromEvent<UserRegisteredWithCustomId>(key: "NonExistentProperty")]
record UserProfileWithInvalidKey(
    [Key]
    Guid Id,

    string Email,

    string Name);
#pragma warning restore SA1402 // File may only contain a single type
