// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBContextStorage.when_beginning_a_context;

public partial class and_a_previous_context_exists : Specification
{
    MongoDBContextStorage _storage;
    string _previousPrefix;
    string _prefix;

    [GeneratedRegex("^c[0-9a-f]{1,8}_$", RegexOptions.None, 1000)]
    private static partial Regex ValidPrefix { get; }

    void Establish()
    {
        _storage = new(_ => throw new Exception("No reader should be created"));
        _previousPrefix = _storage.BeginContext();
    }

    void Because() => _prefix = _storage.BeginContext();

    void Destroy() => _storage.ResetReaders();

    [Fact] void should_not_reuse_the_previous_prefix() => _prefix.ShouldNotEqual(_previousPrefix);
    [Fact] void should_use_a_short_valid_prefix() => ValidPrefix.IsMatch(_prefix).ShouldBeTrue();
    [Fact] void should_use_the_smallest_counter_prefix() => _prefix.ShouldEqual("c2_");
    [Fact] void should_fit_the_longest_named_store_namespace_database() => Encoding.UTF8.GetByteCount($"{_prefix}pii-namespace-scoping+es+pii-namespace-scoping-other").ShouldBeLessThan(64);
    [Fact] void should_fit_the_longest_generated_store_database() => Encoding.UTF8.GetByteCount($"{_prefix}pinned-{new string('a', 32)}+es+Default").ShouldBeLessThan(64);
    [Fact] void should_fit_the_reader_change_database() => Encoding.UTF8.GetByteCount($"{_prefix}{Constants.EventStoreNamespaceDatabaseName}-changes").ShouldBeLessThan(64);
    [Fact] void should_fit_the_cluster_database() => Encoding.UTF8.GetByteCount($"{_prefix}chronicle+main").ShouldBeLessThan(64);
    [Fact] void should_fit_the_reserved_confidentiality_database() => Encoding.UTF8.GetByteCount($"{_prefix}!chronicle-encrypted!+es+Default").ShouldBeLessThan(64);
}
