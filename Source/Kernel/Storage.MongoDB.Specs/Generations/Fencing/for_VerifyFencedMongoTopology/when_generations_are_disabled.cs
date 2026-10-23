// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_VerifyFencedMongoTopology;

public class when_generations_are_disabled : Specification
{
    IServiceProvider _services;

    void Establish() => _services = Substitute.For<IServiceProvider>();

    async Task Because() => await new VerifyFencedMongoTopology(Options.Create(new ChronicleOptions()), _services).Execute(CancellationToken.None);

    [Fact] void should_not_resolve_or_probe_mongo() => _services.DidNotReceive().GetService(Arg.Any<Type>());
}
