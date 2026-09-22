// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleaser.when_releasing_a_sequence;

/// <summary>
/// Whether a read model has anything to release is a property of its type, not of any one instance, so the schema must
/// be asked for once for the whole sequence rather than once per item. Asking per item is what made an observable
/// query's every emission regenerate the same schema once per row.
/// </summary>
public class and_the_read_model_has_no_compliance_metadata : given.a_read_model_releaser
{
    IEnumerable<Order> _instances;
    IEnumerable<Order> _result;

    void Establish() => _instances =
    [
        new(Guid.NewGuid(), "Alice"),
        new(Guid.NewGuid(), "Bob"),
        new(Guid.NewGuid(), "Chandra")
    ];

    async Task Because() => _result = await _releaser.Release(_instances);

    [Fact] void should_generate_the_schema_once_for_the_whole_sequence() => _schemaGenerator.Received(1).Generate(typeof(Order));
    [Fact] void should_hand_back_the_very_same_sequence() => ReferenceEquals(_instances, _result).ShouldBeTrue();
}
