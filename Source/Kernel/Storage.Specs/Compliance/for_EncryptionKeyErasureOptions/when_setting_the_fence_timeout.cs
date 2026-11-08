// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyErasureOptions;

public class when_setting_the_fence_timeout : Specification
{
    readonly EncryptionKeyErasureOptions _options = new();

    [Fact] void should_default_to_a_few_seconds() => _options.FenceTimeout.ShouldEqual(TimeSpan.FromSeconds(3));
    [Fact] void should_refuse_zero() => Catch.Exception(() => _options.FenceTimeout = TimeSpan.Zero).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_refuse_a_negative_timeout() => Catch.Exception(() => _options.FenceTimeout = TimeSpan.FromSeconds(-1)).ShouldBeOfExactType<ArgumentOutOfRangeException>();
}
