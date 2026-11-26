// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_StorageRunOptions;

public class when_parsing_an_unknown_argument : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => StorageRunOptions.Parse(["--nope"]));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<ArgumentException>();
}
