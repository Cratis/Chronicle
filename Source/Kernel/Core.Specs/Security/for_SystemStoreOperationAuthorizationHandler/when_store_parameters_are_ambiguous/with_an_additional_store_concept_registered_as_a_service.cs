// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler.when_store_parameters_are_ambiguous;

public class with_an_additional_store_concept_registered_as_a_service : given.a_query_operation_policy
{
    void Establish()
    {
        _enforce = true;
        var classification = Substitute.For<IServiceProviderIsService>();
        classification.IsService(typeof(EventStoreName)).Returns(true);
        Query(typeof(given.operation_queries).GetMethod(nameof(given.operation_queries.AnotherStoreType))!, new() { ["eventStore"] = "application" }, classification);
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_still_refuse_the_additional_caller_bindable_store() => _context.HasSucceeded.ShouldBeFalse();
}
