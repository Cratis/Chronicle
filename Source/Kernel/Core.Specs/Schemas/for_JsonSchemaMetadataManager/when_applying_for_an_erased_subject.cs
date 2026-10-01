// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

/// <summary>
/// Appending an event for an erased subject keeps refusing: only read models store the erased placeholder.
/// </summary>
public class when_applying_for_an_erased_subject : given.a_value_handler_for_an_erased_subject
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => _manager.Apply(EventStoreName.NotSet, EventStoreNamespaceName.Default, _schema, Identifier, _input));

    [Fact] void should_fail_with_the_compliance_action_exception() => _exception.ShouldBeOfExactType<SchemaMetadataActionFailed>();
    [Fact] void should_carry_the_erasure() => _exception.InnerException.ShouldBeOfExactType<EncryptionKeyErased>();
}
