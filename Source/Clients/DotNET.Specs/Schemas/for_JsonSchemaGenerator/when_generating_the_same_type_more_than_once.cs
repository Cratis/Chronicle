// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

/// <summary>
/// The schema for a type is a function of the CLR type alone, so generating it twice must hand back the very same
/// instance rather than repeating the reflective walk over the type graph.
/// <para>
/// This is not a micro-optimization: the compliance release pass asks for a read model's schema once per *instance*
/// it releases, so an observable query emitting a collection regenerated the identical schema once per item, on every
/// emission, for every subscriber.
/// </para>
/// </summary>
public class when_generating_the_same_type_more_than_once : given.a_json_schema_generator
{
    record Order(Guid Id, string Customer, DateTimeOffset PlacedAt);

    JsonSchema _first;
    JsonSchema _second;

    void Because()
    {
        _first = _generator.Generate(typeof(Order));
        _second = _generator.Generate(typeof(Order));
    }

    [Fact] void should_hand_back_the_same_instance() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_still_describe_the_type() => _second.GetFlattenedProperties().Select(_ => _.Name).ShouldContain(nameof(Order.Customer));
}
