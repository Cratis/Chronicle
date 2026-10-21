// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_Occurrences.when_looking_up_a_receipt;

public class without_a_receipt : given.namespace_bound_occurrences
{
    EffectReceipt _receipt;

    async Task Because() => _receipt = await _occurrences.GetEffectReceipt(_key);

    [Fact] void should_report_no_stored_effect() => _receipt.ShouldBeNull();
}
