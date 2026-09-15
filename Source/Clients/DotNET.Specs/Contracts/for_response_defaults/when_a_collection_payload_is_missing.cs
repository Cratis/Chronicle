// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Chronicle.Contracts.for_response_defaults;

public class when_a_collection_payload_is_missing
{
    [Fact] void should_initialize_enumerables() => Verify<IEnumerable<string>>();
    [Fact] void should_initialize_collections() => Verify<ICollection<string>>();
    [Fact] void should_initialize_lists() => Verify<IList<string>>();
    [Fact] void should_initialize_read_only_collections() => Verify<IReadOnlyCollection<string>>();
    [Fact] void should_initialize_read_only_lists() => Verify<IReadOnlyList<string>>();
    [Fact] void should_initialize_sets() => Verify<ISet<string>>();
    [Fact] void should_initialize_read_only_sets() => Verify<IReadOnlySet<string>>();
    [Fact] void should_initialize_dictionaries() => Verify<IDictionary<string, string>>();
    [Fact] void should_initialize_read_only_dictionaries() => Verify<IReadOnlyDictionary<string, string>>();
    [Fact] void should_initialize_arrays() => Verify<string[]>();
    [Fact] void should_initialize_concrete_lists() => Verify<List<string>>();
    [Fact] void should_initialize_concrete_dictionaries() => Verify<Dictionary<string, string>>();

    static void Verify<T>()
        where T : class, IEnumerable
    {
        var query = QueryResult<T>.Success(Guid.Empty, null!);
        query.Data.ShouldNotBeNull();
        query.Data.ShouldBeEmpty();
        query.IsSuccess.ShouldBeTrue();

        var command = CommandResult<T>.Success(Guid.Empty, null!);
        command.Response.ShouldNotBeNull();
        command.Response.ShouldBeEmpty();
        command.IsSuccess.ShouldBeTrue();

        var failure = new InvalidOperationException("The operation failed");
        var failedQuery = QueryResult<T>.Error(Guid.Empty, failure);
        failedQuery.Data.ShouldNotBeNull();
        failedQuery.Data.ShouldBeEmpty();
        failedQuery.IsSuccess.ShouldBeFalse();
        failedQuery.ExceptionMessages.ShouldContainOnly(failure.Message);

        var failedCommand = CommandResult<T>.Error(Guid.Empty, failure);
        failedCommand.Response.ShouldNotBeNull();
        failedCommand.Response.ShouldBeEmpty();
        failedCommand.IsSuccess.ShouldBeFalse();
        failedCommand.ExceptionMessages.ShouldContainOnly(failure.Message);
    }
}
