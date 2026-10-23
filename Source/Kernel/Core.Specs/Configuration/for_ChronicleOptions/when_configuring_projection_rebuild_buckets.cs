// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions;

public class when_configuring_projection_rebuild_buckets : Specification
{
    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    void should_accept_the_boundary_values(int count) => new ChronicleOptions { Jobs = new() { ProjectionRebuildBuckets = count } }
        .Validate(new ValidationContext(new object())).ShouldBeEmpty();

    [Theory]
    [InlineData(-1)]
    [InlineData(65)]
    void should_reject_out_of_range_values(int count) => new ChronicleOptions { Jobs = new() { ProjectionRebuildBuckets = count } }
        .Validate(new ValidationContext(new object())).Single().ErrorMessage.ShouldEqual("Jobs.ProjectionRebuildBuckets must be between 0 and 64.");

    [Fact] void should_default_to_ordered_rebuilds() => new Jobs().ProjectionRebuildBuckets.ShouldEqual(0);
}
