// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

/// <summary>
/// A read model is derived state, so a protected value whose subject has been erased is stored as the erased
/// placeholder rather than failing the update and freezing the partition (#4453) - whatever value arrived.
/// </summary>
public class when_applying_to_a_read_model_for_an_erased_subject : given.a_value_handler_for_an_erased_subject
{
    JsonObject _result;

    async Task Because() => _result = await _manager.ApplyToReadModel(EventStoreName.NotSet, EventStoreNamespaceName.Default, _schema, Identifier, _input);

    [Fact] void should_store_the_placeholder_for_a_scalar() => _result["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_placeholder_for_a_value_object_marked_as_a_whole() => _result["address"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_placeholder_for_a_list_element() => _result["emails"]![0]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_leave_the_unprotected_property_alone() => _result["status"]!.GetValue<string>().ShouldEqual("active");
    [Fact] void should_keep_no_personal_data() => _result.ToJsonString().ShouldNotContain("Ada");
}
