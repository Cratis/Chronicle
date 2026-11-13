// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using context = Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_restricting_source_namespaces.to_one_of_two_target_namespaces.context;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_restricting_source_namespaces;

[Collection(ChronicleCollection.Name)]
public class to_one_of_two_target_namespaces(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public IEnumerable<string> SourceNamespaces;

        public override IEnumerable<Type> EventTypes => [typeof(SubscriptionHistoryPublished)];

        async Task Because()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var observed = $"observed-{suffix}";
            var other = $"other-{suffix}";
            var source = await ChronicleClient.GetEventStore($"source-{suffix}", observed);
            var target = await ChronicleClient.GetEventStore($"target-{suffix}", observed);

            // The target has a second namespace that the restricted subscription must not mirror into the source.
            await ChronicleClient.GetEventStore(target.Name, other);
            await source.RegisterAll();
            await target.RegisterAll();

            await target.Subscriptions.Subscribe(
                "restricted",
                source.Name,
                builder => builder.WithEventType<SubscriptionHistoryPublished>().WithSourceNamespace(observed));

            var namespaces = Services.GetRequiredService<IGrainFactory>().GetGrain<INamespaces>(source.Name.Value);
            SourceNamespaces = (await namespaces.GetAll()).Select(_ => _.Value).ToArray();
        }
    }

    [Fact] void should_not_create_the_other_namespace_in_the_source_event_store() => Context.SourceNamespaces.Any(name => name.StartsWith("other-")).ShouldBeFalse();
}
