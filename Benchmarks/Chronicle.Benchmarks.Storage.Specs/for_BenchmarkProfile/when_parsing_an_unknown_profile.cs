// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_BenchmarkProfile;

public class when_parsing_an_unknown_profile : Specification
{
    Exception? _exception;

    void Because() => _exception = Catch.Exception(() => BenchmarkProfile.Parse("unknown"));

    [Fact] void should_reject_the_name() => _exception.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_list_all_profiles() => _exception!.Message.ShouldContain("Use smoke, short or medium.");
}
