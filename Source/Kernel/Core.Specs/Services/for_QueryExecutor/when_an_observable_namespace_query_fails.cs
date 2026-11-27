// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Services.for_QueryExecutor;

public class when_an_observable_namespace_query_fails : Specification
{
    QueryResult<int> _result;

    async Task Because() => _result = await QueryExecutor.Execute(() => Observable.Throw<int>(new NamespaceClosing("tenant"))).FirstAsync();

    [Fact] void should_return_the_literal_code() => _result.ExceptionMessages.ShouldEqual<string>(["NamespaceClosing"]);
    [Fact] void should_not_return_an_exception_stack() => _result.ExceptionStackTrace.ShouldBeEmpty();
}
