// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionAccess.given;

public class a_connection_check : Specification
{
    protected const string Id = "connection";
    protected static readonly ConnectionOwner Alice = new("issuer", "alice");
    protected static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);
    protected ConnectionOwnership _ownership;
    protected IGrainFactory _factory;
    protected IConnectionReservation _reservation;
    protected ConnectionAccessOutcome _result;

    void Establish()
    {
        _ownership = new();
        _factory = Substitute.For<IGrainFactory>();
        _reservation = Substitute.For<IConnectionReservation>();
        _factory.GetGrain<IConnectionReservation>(Arg.Any<string>(), Arg.Any<string>()).Returns(_reservation);
        _factory.ClearReceivedCalls();
    }
}
