// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Integration.for_OldKernelOccurrences;

[Collection(OldKernelCollection.Name)]
public class when_enrolling_a_receipted_unit_of_work(OldKernelFixture fixture)
{
    [Fact]
    public async Task should_fail_closed_before_sending_any_receipt()
    {
        var registration = await fixture.EventStore.WaitForRegistration().WaitAsync(TimeSpan.FromSeconds(30));
        registration.IsSuccess.ShouldBeTrue();
        using var unit = new UnitOfWorkManager(fixture.EventStore).Begin(CorrelationId.New());
        var key = new EffectKey(new("old-occurrence"), new("effect"), new(new("old-occurrence"), 1));
        var error = Catch.Exception(() => unit.UseEffectReceipt(key));
        error.ShouldBeOfExactType<EffectReceiptsNotSupported>();
        fixture.Requests.Paths.Any(path => path.Contains("Register", StringComparison.Ordinal)).ShouldBeTrue();
        fixture.Requests.Paths.Where(path => path.Contains("Occurrence", StringComparison.Ordinal)
            || path.Contains("EffectReceipt", StringComparison.Ordinal)
            || path.Contains("/Append", StringComparison.Ordinal)).ShouldBeEmpty();
    }
}
