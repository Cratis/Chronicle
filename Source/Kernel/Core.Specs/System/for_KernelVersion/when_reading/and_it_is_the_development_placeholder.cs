// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sys.for_KernelVersion.when_reading;

public class and_it_is_the_development_placeholder : Specification
{
    [Theory]
    [InlineData("1.0.0")]
    [InlineData("0.0.0")]
    [InlineData("1.0.0+abc123")]
    [InlineData("0.0.0+abc123")]
    void should_treat_the_version_as_unknown(string informationalVersion) => KernelVersion.IsKnown(KernelVersion.Parse(informationalVersion)).ShouldBeFalse();
}
