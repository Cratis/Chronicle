// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_supplying_a_default_service_provider : given.schema_sharing
{
    EventStoreForTesting _derived;

    void Because()
    {
        _other = new(new DefaultServiceProvider(), _artifacts);
        _derived = new(Substitute.For<DefaultServiceProvider>(), _artifacts);
    }

    [Fact] void should_share_for_the_exact_default_provider_type() => GeneratorFor(_other).ShouldBeSame(GeneratorFor(_store));
    [Fact] void should_not_share_for_a_derived_default_provider_type() => ReferenceEquals(GeneratorFor(_derived), GeneratorFor(_store)).ShouldBeFalse();
    [Fact] void should_preserve_generated_schemas() => SchemaFor(_other).ToJson().ShouldEqual(SchemaFor(_store).ToJson());

    void Destroy() => _derived?.Dispose();
}
