// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Services.for_QueryExecutor;

public class when_a_namespace_query_has_a_wrapped_failure : Specification
{
    QueryResult<int> _result;
    Exception _observed;
    Exception _failure;

    void Establish() => _failure = new TargetInvocationException(new NamespaceRecoveryRequired("private-tenant"));

    async Task Because() => _result = await QueryExecutor.Execute(() => Task.FromException<int>(_failure), error => _observed = error);

    [Fact] void should_return_the_literal_code() => _result.ExceptionMessages.ShouldEqual<string>(["NamespaceRecoveryRequired"]);
    [Fact] void should_not_return_an_exception_stack() => _result.ExceptionStackTrace.ShouldBeEmpty();
    [Fact] void should_preserve_the_original_callback_failure() => _observed.ShouldBeSame(_failure);
}
