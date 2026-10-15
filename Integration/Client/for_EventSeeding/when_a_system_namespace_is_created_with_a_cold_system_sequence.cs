// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Orleans.Core.Internal;
using context = Cratis.Chronicle.Integration.for_EventSeeding.when_a_system_namespace_is_created_with_a_cold_system_sequence.context;

namespace Cratis.Chronicle.Integration.for_EventSeeding;

[Collection(ChronicleCollection.Name)]
public class when_a_system_namespace_is_created_with_a_cold_system_sequence(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public IEnumerable<Concepts.EventStoreNamespaceName> Namespaces;

        async Task Establish()
        {
            var factory = Services.GetRequiredService<IGrainFactory>();
            var sequence = factory.GetSystemEventSequence();
            await sequence.AsReference<IGrainManagementExtension>().DeactivateOnIdle().AsTask()
                .WaitAsync(TimeSpan.FromSeconds(10));
        }

        async Task Because()
        {
            var factory = Services.GetRequiredService<IGrainFactory>();
            var namespaces = factory.GetGrain<Cratis.Chronicle.Namespaces.INamespaces>(Concepts.EventStoreName.System.Value);
            await namespaces.Ensure("cold-sequence").WaitAsync(TimeSpan.FromSeconds(10));
            Namespaces = await namespaces.GetAll();
        }
    }

    [Fact] void should_create_the_namespace_without_an_activation_deadlock() => Context.Namespaces.ShouldContain((Concepts.EventStoreNamespaceName)"cold-sequence");
}
