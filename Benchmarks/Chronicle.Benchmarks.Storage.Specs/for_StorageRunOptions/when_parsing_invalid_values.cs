// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_StorageRunOptions;

public class when_parsing_invalid_values : Specification
{
    Exception? _repetition;
    Exception? _payload;
    Exception? _notANumber;

    void Because()
    {
        _repetition = Catch.Exception(() => StorageRunOptions.Parse(["--repetition", "0"]));
        _payload = Catch.Exception(() => StorageRunOptions.Parse(["--payload-bytes", "0"]));
        _notANumber = Catch.Exception(() => StorageRunOptions.Parse(["--max-load", "abc"]));
    }

    [Fact] void should_reject_zero_repetition() => _repetition.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_zero_payload() => _payload.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_non_numeric_load() => _notANumber.ShouldBeOfExactType<ArgumentException>();
}
