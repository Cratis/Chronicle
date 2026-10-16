// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.EventSequences.for_ConstraintIndexRebuildRunId;

public class when_round_tripping_between_silos : Specification
{
    readonly ConstraintIndexRebuildRunId _original = new(Guid.NewGuid(), Guid.NewGuid());
    ConstraintIndexRebuildRunId _result;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSerializer(builder => builder.Services.AddConceptSerializer());
        using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<Serializer>();
        _result = serializer.Deserialize<ConstraintIndexRebuildRunId>(serializer.SerializeToArray(_original));
    }

    [Fact] void should_preserve_the_step_identity() => _result.Value.ShouldEqual(_original.Value);
    [Fact] void should_preserve_the_attempt_nonce() => _result.Attempt.ShouldEqual(_original.Attempt);
}
