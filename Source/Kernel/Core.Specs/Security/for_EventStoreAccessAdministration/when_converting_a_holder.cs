// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services.Security;

namespace Cratis.Chronicle.Security.for_EventStoreAccessAdministration;

public class when_converting_a_holder : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_preserve_the_exact_issuer_subject_and_subject_kind(bool isClient)
    {
        var holder = new EventStoreAccessHolder("Issuer", "Subject", isClient);
        holder.ToContract().ToApi().ShouldEqual(holder);
    }
}
