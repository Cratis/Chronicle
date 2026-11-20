// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_StorageRunOptions;

public class when_parsing_arguments : Specification
{
    StorageRunOptions _options = default!;

    void Because() => _options = StorageRunOptions.Parse(["--profile", "smoke", "--providers", "sqlite,pg,mongo", "--max-load", "4", "--repetition", "2"]);

    [Fact] void should_select_the_profile() => _options.Profile.Name.ShouldEqual("smoke");
    [Fact] void should_select_the_providers_in_order() => string.Join(',', _options.Providers).ShouldEqual(string.Join(',', new[] { StorageProvider.Sqlite, StorageProvider.PostgreSql, StorageProvider.Mongo }));
    [Fact] void should_apply_the_load_limit() => _options.MaxLoad.ShouldEqual(4.0);
    [Fact] void should_record_the_repetition() => _options.Repetition.ShouldEqual(2);
}
