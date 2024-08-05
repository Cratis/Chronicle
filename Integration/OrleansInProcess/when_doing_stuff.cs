// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Orleans.Aggregates;

namespace Cratis.Chronicle.Integration.OrleansInProcess;

[Collection(GlobalCollection.Name)]
public class when_doing_stuff : OrleansTest
{
    public when_doing_stuff(OrleansFixture fixture) : base(fixture)
    {
    }

    [Fact]
    void should_be_awesome()
    {
        var factory = Fixture.Services.GetRequiredService<IAggregateRootFactory>();

        true.ShouldBeTrue();
    }
}
