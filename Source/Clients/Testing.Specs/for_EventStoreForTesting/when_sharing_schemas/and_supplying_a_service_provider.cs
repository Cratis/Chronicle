// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_supplying_a_service_provider : given.schema_sharing
{
    EventStoreForTesting _first;

    void Because()
    {
        _first = new(Substitute.For<IServiceProvider>(), _artifacts);
        _other = new(Substitute.For<IServiceProvider>(), _artifacts);
    }

    [Fact] void should_not_share_with_the_default_service_provider() => ReferenceEquals(GeneratorFor(_store), GeneratorFor(_other)).ShouldBeFalse();
    [Fact] void should_not_share_between_supplied_service_providers() => ReferenceEquals(GeneratorFor(_first), GeneratorFor(_other)).ShouldBeFalse();
    [Fact] void should_preserve_generated_schemas() => SchemaFor(_store).ToJson().ShouldEqual(SchemaFor(_other).ToJson());

    void Destroy() => _first?.Dispose();
}
