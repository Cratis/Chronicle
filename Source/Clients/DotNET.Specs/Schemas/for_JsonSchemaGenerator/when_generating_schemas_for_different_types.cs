// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

/// <summary>
/// Memoizing by type must key on the type - two different types must not share a schema, which is the failure mode a
/// naive single-entry cache would introduce.
/// </summary>
public class when_generating_schemas_for_different_types : given.a_json_schema_generator
{
    record Order(Guid Id, string Customer);
    record Shipment(Guid Id, string Carrier);

    JsonSchema _order;
    JsonSchema _shipment;

    void Because()
    {
        _order = _generator.Generate(typeof(Order));
        _shipment = _generator.Generate(typeof(Shipment));
    }

    [Fact] void should_not_share_an_instance() => ReferenceEquals(_order, _shipment).ShouldBeFalse();
    [Fact] void should_describe_the_first_type() => _order.GetFlattenedProperties().Select(_ => _.Name).ShouldContain(nameof(Order.Customer));
    [Fact] void should_describe_the_second_type() => _shipment.GetFlattenedProperties().Select(_ => _.Name).ShouldContain(nameof(Shipment.Carrier));
}
