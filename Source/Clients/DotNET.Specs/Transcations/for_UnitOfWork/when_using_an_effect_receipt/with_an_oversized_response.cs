// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_using_an_effect_receipt;

public class with_an_oversized_response : given.a_receipted_unit
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _unitOfWork.UseEffectReceipt(_key, JsonValue.Create(new string('a', 65535))));

    [Fact] void should_count_the_json_quotes_in_the_limit() => _exception.ShouldBeOfExactType<EffectReceiptResponseTooLarge>();
}
