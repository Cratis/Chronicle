// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;

namespace Cratis.Chronicle.Setup.for_BusySilos.given;

public class a_cluster : Specification
{
    protected SiloAddress _thisSilo;
    protected SiloAddress _activeSibling;
    protected SiloAddress _deadSibling;
    protected BusySilos _busySilos;
    protected BusySilo? _result;

    void Establish()
    {
        _thisSilo = SiloAddress.New(new IPEndPoint(IPAddress.Parse("10.0.0.1"), 11111), 42);
        _activeSibling = SiloAddress.New(new IPEndPoint(IPAddress.Parse("10.0.0.2"), 11111), 7);
        _deadSibling = SiloAddress.New(new IPEndPoint(IPAddress.Parse("10.0.0.3"), 11111), 3);

        var localSilo = Substitute.For<ILocalSiloDetails>();
        localSilo.SiloAddress.Returns(_thisSilo);
        var membership = Substitute.For<IClusterMembershipService>();
        membership.CurrentSnapshot.Returns(new ClusterMembershipSnapshot(
            new Dictionary<SiloAddress, ClusterMember>
            {
                [_thisSilo] = new(_thisSilo, SiloStatus.Active, "this"),
                [_activeSibling] = new(_activeSibling, SiloStatus.Active, "active"),
                [_deadSibling] = new(_deadSibling, SiloStatus.Dead, "dead")
            }.ToImmutableDictionary(),
            new MembershipVersion(1)));

        _busySilos = new(localSilo, membership);
    }

    /// <summary>
    /// Creates the timeout Orleans reports for a request that got no answer, describing the request with Orleans' own
    /// message formatting.
    /// </summary>
    /// <param name="target">The target silo of the request; <see langword="null"/> when it was not resolved yet.</param>
    /// <returns>The <see cref="TimeoutException"/>.</returns>
    /// <remarks>
    /// The text around the message is how Orleans 10.4.2's CallbackData words the timeout. The message itself is a real
    /// Orleans Message - an internal type, reached by reflection - so a change to how Orleans describes a request
    /// fails these specs rather than silently turning off the busy-silo wait.
    /// </remarks>
    protected TimeoutException TimeoutTo(SiloAddress? target) =>
        new($"Response did not arrive on time in {TimeSpan.FromSeconds(30)} for message: {Request(target)}. ");

    /// <summary>
    /// Creates a timeout whose request names its target as given, for target shapes Orleans itself does not write.
    /// </summary>
    /// <param name="target">The target as written in the request.</param>
    /// <returns>The <see cref="TimeoutException"/>.</returns>
    protected TimeoutException TimeoutNaming(string target) =>
        new($"Response did not arrive on time in 00:00:30 for message: Request [{_thisSilo} sys.svc.user/startup]->[{target} eventstoresubscriptionsmanager/Studio] #1234. ");

    string Request(SiloAddress? target)
    {
        var messageType = typeof(IClusterClient).Assembly.GetType("Orleans.Runtime.Message", throwOnError: true)!;
        var message = Activator.CreateInstance(messageType, nonPublic: true)!;
        var directions = messageType.GetProperty("Direction")!.PropertyType;
        messageType.GetProperty("Direction")!.SetValue(message, Enum.Parse(directions, "Request"));
        messageType.GetProperty("SendingSilo")!.SetValue(message, _thisSilo);
        messageType.GetProperty("SendingGrain")!.SetValue(message, GrainId.Create("sys.svc.user", "startup"));
        messageType.GetProperty("TargetSilo")!.SetValue(message, target);
        messageType.GetProperty("TargetGrain")!.SetValue(message, GrainId.Create("eventstoresubscriptionsmanager", "Studio"));
        return $"{message}";
    }
}
