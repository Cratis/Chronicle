// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_starting;

public class and_it_is_disabled : Specification
{
    readonly SingleWriterLease _options = new() { Enabled = false };

    [Fact] void should_not_apply_even_to_remote_persistent_storage() => _options.AppliesTo(ClusteringType.Localhost, "MsSql").ShouldBeFalse();
}
