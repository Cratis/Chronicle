// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_a_root_join_also_has_direct_property_changes : given.a_root_join_with_direct_property_changes
{
    IEnumerable<ExpandoObject> _result;

    async Task Because() => _result = await ProcessForMultipleReadModels(CreatedEvent, RootJoinEvent);

    [Fact] void should_apply_the_joined_properties() => ((IDictionary<string, object?>)_result.Single())["CustomerName"].ShouldEqual("Ada");
    [Fact] void should_not_apply_the_direct_properties() => ((IDictionary<string, object?>)_result.Single())["Name"].ShouldEqual("Original");
}
