// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing.given;

/// <summary>
/// Activates one projection, with a join and children, in several namespaces of one event store, each holding different data
/// under the very same event source ids, and waits until every namespace has projected it.
/// </summary>
/// <param name="fixture">The isolated integration fixture.</param>
public class a_projection_in_many_namespaces(ProjectionPlanSharingFixture fixture) : Specification<ProjectionPlanSharingFixture>(fixture)
{
    public const string OrderId = "order-1";
    public const string CustomerId = "customer-1";
    public static readonly string[] Namespaces = ["tenant-a", "tenant-b", "tenant-c"];

    protected IGrainFactory _grains;
    protected string _storeName;
    protected string _projectionId;
    protected Dictionary<string, IEventStore> _stores = [];

    public override bool AutoDiscoverArtifacts => false;

    public override IEnumerable<Type> Projections => [typeof(SharedOrderProjection)];

    public override IEnumerable<Type> EventTypes => [typeof(SharedCustomerRegistered), typeof(SharedOrderPlaced), typeof(SharedLineAdded)];

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new SharedOrderProjection());

    protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);

    /// <summary>
    /// Gets the number of compiled plans the kernel holds for the projection, however many namespaces run it.
    /// </summary>
    protected int PlansHeld => ((Cratis.Chronicle.Projections.Engine.ProjectionFactory)Services.GetRequiredService<Cratis.Chronicle.Projections.Engine.IProjectionFactory>())
        .CachedPlanCountFor(_storeName, _projectionId);

    protected static string CustomerNameIn(string @namespace) => $"customer-in-{@namespace}";

    protected static string LabelIn(string @namespace) => $"label-in-{@namespace}";

    protected static int LinesIn(string @namespace) => Array.IndexOf(Namespaces, @namespace) + 1;

    protected static string SkuIn(string @namespace, int line) => $"{@namespace}-sku-{line}";

    /// <summary>
    /// Polls until the read model of the order in a namespace satisfies the condition.
    /// </summary>
    /// <param name="namespace">The namespace to read in.</param>
    /// <param name="condition">The condition the read model must satisfy.</param>
    /// <param name="orderId">The order to read.</param>
    /// <returns>The read model, or the last one seen when the condition never held.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no read model was ever read.</exception>
    protected async Task<SharedOrder> WaitForOrder(string @namespace, Func<SharedOrder, bool> condition, string orderId = OrderId)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        SharedOrder? last = null;
        Exception? notProjectedYet = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                last = await _stores[@namespace].ReadModels.GetInstanceById<SharedOrder>(orderId);
                if (last is not null && condition(last)) return last;
            }
            catch (Exception exception)
            {
                notProjectedYet = exception;
            }

            await Task.Delay(250);
        }

        return last ?? throw new InvalidOperationException("The read model was never projected.", notProjectedYet);
    }

    protected static bool IsComplete(SharedOrder order, string @namespace) =>
        order.Label == LabelIn(@namespace) &&
        order.CustomerName == CustomerNameIn(@namespace) &&
        order.Lines?.Count() == LinesIn(@namespace);

    /// <summary>
    /// Registers the definition of the projection again with one more mapping, so that the summary is filled from the label.
    /// </summary>
    /// <returns>An awaitable task.</returns>
    protected async Task ChangeDefinition()
    {
        var manager = _grains.GetGrain<IProjectionsManager>(_storeName);
        var current = (await manager.GetProjectionDefinitions()).Single(definition => definition.Identifier.Value == _projectionId);
        var from = current.From.ToDictionary(
            pair => pair.Key,
            pair =>
            {
                var label = pair.Value.Properties.Where(property => property.Key.Path.Equals("Label", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (label.Length == 0) return pair.Value;
                var summary = new PropertyPath(char.IsUpper(label[0].Key.Path[0]) ? "Summary" : "summary");
                return pair.Value with { Properties = new Dictionary<PropertyPath, string>(pair.Value.Properties) { [summary] = label[0].Value } };
            });
        await manager.Register([current with { From = from }]);
    }

    /// <summary>
    /// Replays the observer of the projection in every namespace, as an operator does after a definition change.
    /// </summary>
    /// <returns>An awaitable task.</returns>
    protected async Task ReplayEveryNamespace()
    {
        foreach (var @namespace in Namespaces)
        {
            await _grains.GetGrain<Cratis.Chronicle.Observation.IObserver>(new ObserverKey(_projectionId, _storeName, @namespace, EventSequenceId.Log)).Replay();
        }
    }

    protected async Task AppendOrderIn(string @namespace)
    {
        var log = _stores[@namespace].EventLog;
        (await log.Append(OrderId, new SharedOrderPlaced(CustomerId, LabelIn(@namespace)))).IsSuccess.ShouldBeTrue();
        for (var line = 1; line <= LinesIn(@namespace); line++)
        {
            (await log.Append(OrderId, new SharedLineAdded(OrderId, SkuIn(@namespace, line), line))).IsSuccess.ShouldBeTrue();
        }
    }

    async Task Establish()
    {
        _storeName = $"plans-{Guid.NewGuid():N}";
        _grains = Services.GetRequiredService<IGrainFactory>();
        foreach (var @namespace in Namespaces)
        {
            var store = await ChronicleClient.GetEventStore(_storeName, @namespace);
            await store.DiscoverAll();
            _stores[@namespace] = store;
        }

        var first = _stores[Namespaces[0]];
        _projectionId = first.Projections.GetProjectionIdFor<SharedOrderProjection>().Value;
        await first.RegisterAll();
        (await first.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();

        // Each namespace comes into existence through its first append, before the projection is registered.
        foreach (var @namespace in Namespaces)
        {
            (await _stores[@namespace].EventLog.Append(CustomerId, new SharedCustomerRegistered(CustomerNameIn(@namespace)))).IsSuccess.ShouldBeTrue();
        }

        foreach (var @namespace in Namespaces)
        {
            await AppendOrderIn(@namespace);
        }

        foreach (var @namespace in Namespaces)
        {
            var order = await WaitForOrder(@namespace, candidate => IsComplete(candidate, @namespace));
            order.ShouldNotBeNull();
            if (!IsComplete(order, @namespace))
            {
                throw new InvalidOperationException($"Namespace {@namespace} did not project completely: {System.Text.Json.JsonSerializer.Serialize(order)}");
            }
        }
    }
}
