// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.GraphProjections.Organization;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Neo4j;
using context = Cratis.Chronicle.Integration.GraphProjections.when_isolating_namespaces.and_two_tenants_use_the_same_keys.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_isolating_namespaces;

[Collection(GraphChronicleCollection.Name)]
public class and_two_tenants_use_the_same_keys(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        public const string TenantA = "tenant-a";
        public const string TenantB = "tenant-b";

        readonly Dictionary<string, GraphSnapshot> _perNamespace = [];

        /// <summary>
        /// Gets the nodes of a namespace: the nodes stamped with it in single database mode, and the nodes of its own database otherwise.
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <returns>The nodes.</returns>
        public IReadOnlyList<GraphNode> NodesOf(string @namespace) =>
            IsSingleDatabase ? [.. Graph.Nodes.Where(_ => _.Namespace == @namespace)] : _perNamespace[@namespace].Nodes;

        async Task Because()
        {
            await AppendTo(TenantA, "Ada in A", 5);
            await AppendTo(TenantB, "Ada in B", 7);
            await ReadGraph();
            if (!IsSingleDatabase)
            {
                foreach (var @namespace in new[] { TenantA, TenantB })
                {
                    await ReadGraph(GraphDatabaseName.For(null, Constants.EventStore, @namespace));
                    _perNamespace[@namespace] = Graph;
                }
            }
        }

        async Task AppendTo(string @namespace, string name, int points)
        {
            var store = await ChronicleClient.GetEventStore(Constants.EventStore, @namespace);
            await store.DiscoverAll();
            await store.RegisterAll();
            await Task.WhenAll(
                Wait(await store.EventLog.Append(OrganizationStory.Ada.ToString(), new OrgPersonRegistered(OrganizationStory.Ada, name, []))),
                Wait(await store.EventLog.Append(OrganizationStory.Ada.ToString(), new OrgPointsAwarded(OrganizationStory.Ada, points))));
        }

        static async Task Wait(IAppendResult result) => (await ((IAppendResultForObserverCompletion)result).WaitForCompletion(TimeSpan.FromSeconds(60))).IsSuccess.ShouldBeTrue();
    }

    [Fact] void should_give_each_namespace_its_own_node_for_the_same_key() => (Context.NodesOf(context.TenantA).Count, Context.NodesOf(context.TenantB).Count).ShouldEqual((1, 1));
    [Fact] void should_keep_the_properties_of_the_first_namespace() => (Context.NodesOf(context.TenantA)[0]["name"]?.ToString(), (long?)Context.NodesOf(context.TenantA)[0]["score"]).ShouldEqual(("Ada in A", 5L));
    [Fact] void should_keep_the_properties_of_the_second_namespace() => (Context.NodesOf(context.TenantB)[0]["name"]?.ToString(), (long?)Context.NodesOf(context.TenantB)[0]["score"]).ShouldEqual(("Ada in B", 7L));
}
