// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModelLookups.for_ReadModelLookup.when_finding_by_composite_key;

public class and_the_projection_is_not_readable : given.a_composite_key_read
{
    readonly List<Exception> _errors = [];
    int _storeCallsBeforeSupportedRead;

    async Task Because()
    {
        var definition = _projections.Single();
        foreach (var owner in Enum.GetValues<ProjectionOwner>().Where(_ => _ != ProjectionOwner.Kernel))
        {
            foreach (var sequence in new[] { "system", "SYSTEM", " SyStEm ", "\tSyStEm\r\n" })
            {
                _projections = [definition with { Owner = owner, EventSequenceId = sequence }];
                _errors.Add(await Catch.Exception(Read));
            }
        }

        _projections = [];
        _errors.Add(await Catch.Exception(Read));
        _storeCallsBeforeSupportedRead = _store.ReceivedCalls().Count();
        _projections = [definition with { Owner = ProjectionOwner.Kernel, EventSequenceId = "system" }];
        await Read();
    }

    [Fact] void should_refuse_every_unsupported_or_missing_producer() => _errors.TrueForAll(_ => _ is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_use_the_neutral_refusal() => _errors.Cast<RpcException>().All(_ => _.Status.Detail == SystemStoreAccess.Refused).ShouldBeTrue();
    [Fact] void should_check_every_variant() => _errors.Count.ShouldEqual((Enum.GetValues<ProjectionOwner>().Count(_ => _ != ProjectionOwner.Kernel) * 4) + 1);
    [Fact] void should_refuse_before_using_the_materialized_store() => _storeCallsBeforeSupportedRead.ShouldEqual(0);
    [Fact] void should_allow_the_kernel_producer() => _result.Outcome.ShouldEqual(ReadModelLookupOutcome.Found);
}
