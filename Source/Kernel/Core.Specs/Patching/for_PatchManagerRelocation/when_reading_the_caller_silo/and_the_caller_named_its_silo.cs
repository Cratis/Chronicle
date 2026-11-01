// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Patching.for_PatchManagerRelocation.when_reading_the_caller_silo;

public class and_the_caller_named_its_silo : Specification
{
    readonly SiloAddress _silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
    SiloAddress? _result;

    void Establish() => RequestContext.Set(PatchManagerRelocation.CallerSiloKey, _silo);
    void Because() => _result = PatchManagerRelocation.CallerSilo();
    void Destroy() => RequestContext.Remove(PatchManagerRelocation.CallerSiloKey);

    [Fact] void should_read_the_silo() => _result.ShouldEqual(_silo);
}
