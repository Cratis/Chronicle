// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_application_metadata;

public class and_supplying_a_service_provider : given.application_metadata_sharing
{
    void Because() => _other = new(Substitute.For<IServiceProvider>(), _artifacts);

    [Fact] void should_not_give_the_store_metadata() => _other.Metadata.ShouldBeNull();
}
