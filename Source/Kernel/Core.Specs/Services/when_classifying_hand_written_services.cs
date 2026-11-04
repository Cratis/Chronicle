// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Chronicle.Services.given.hand_written_routing_surface;

namespace Cratis.Chronicle.Services;

public class when_classifying_hand_written_services : Specification
{
    [Fact]
    void should_classify_every_public_method()
    {
        var unclassified = HandWritten.SelectMany(MethodsOf).Select(KeyOf).Where(key => !Classification.ContainsKey(key)).ToArray();
        unclassified.ShouldBeEmpty();
    }

    [Fact]
    void should_not_list_a_method_that_no_longer_exists()
    {
        var existing = HandWritten.SelectMany(MethodsOf).Select(KeyOf).ToHashSet();
        Classification.Keys.Where(key => !existing.Contains(key)).ToArray().ShouldBeEmpty();
    }

    [Fact]
    void should_give_every_exemption_a_reason()
    {
        Classification.Values.Where(value => value.StartsWith(Exempt, StringComparison.Ordinal)).All(value => value.Length > Exempt.Length + 8).ShouldBeTrue();
    }
}
