// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Represents how the discovered observers are delivered.
/// </summary>
/// <param name="Http">The identifiers of the observers delivered over HTTP.</param>
/// <param name="Stream">The identifiers of the observers delivered over a stream.</param>
/// <param name="UnknownOverrides">The configured override keys that match no discovered observer.</param>
internal sealed record ObserverDeliveryPlan(
    IReadOnlyList<string> Http,
    IReadOnlyList<string> Stream,
    IReadOnlyList<string> UnknownOverrides)
{
    /// <summary>
    /// Creates a plan for the observers.
    /// </summary>
    /// <param name="deliveries">The <see cref="ObserverDeliveries"/> to resolve with.</param>
    /// <param name="observers">The identifiers and optional CLR types of the discovered observers.</param>
    /// <returns>The <see cref="ObserverDeliveryPlan"/>.</returns>
    internal static ObserverDeliveryPlan Create(ObserverDeliveries deliveries, IEnumerable<(string Id, Type? Type)> observers)
    {
        var all = observers.ToArray();
        var http = new List<string>();
        var stream = new List<string>();
        foreach (var (id, type) in all)
        {
            if (deliveries.Resolve(id, type) == ObserverDelivery.Http)
            {
                http.Add(id);
            }
            else
            {
                stream.Add(id);
            }
        }

        return new(http, stream, deliveries.UnknownOverrides(all.Select(_ => _.Id)));
    }

    /// <summary>
    /// Throws when an observer is delivered over HTTP, which this client cannot register yet.
    /// </summary>
    /// <remarks>
    /// The client fails closed: it does not register HTTP-delivered observers whatever the kernel advertises. Lifting this takes all of:
    /// <list type="number">
    /// <item>The kernel advertises an HTTP-delivery capability (added to <c language="csharp">KernelCapabilities</c> by #5116), and the client requires
    /// <c language="csharp">CapabilitiesAreKnown</c> together with that capability.</item>
    /// <item>The kernel drains replay and recovery state before an observer switches from stream to HTTP delivery (#5115, decision 0018).</item>
    /// <item>The receiver exists (#5119).</item>
    /// </list>
    /// </remarks>
    /// <param name="options">The <see cref="ChronicleOptions"/> holding the serverless settings.</param>
    /// <exception cref="InvalidServerlessConfiguration">Thrown when an observer is delivered over HTTP and no receiver URL is configured.</exception>
    /// <exception cref="HttpDeliveryNotAvailable">Thrown when an observer is delivered over HTTP.</exception>
    internal void ThrowIfNotDeliverable(ChronicleOptions options)
    {
        if (Http.Count == 0)
        {
            return;
        }

        if (options.Serverless.ReceiverUrl is null)
        {
            throw new InvalidServerlessConfiguration([$"ReceiverUrl (Cratis:Chronicle:Serverless:ReceiverUrl) is required because observers [{string.Join(", ", Http)}] are delivered over HTTP."]);
        }

        throw new HttpDeliveryNotAvailable(Http);
    }
}
