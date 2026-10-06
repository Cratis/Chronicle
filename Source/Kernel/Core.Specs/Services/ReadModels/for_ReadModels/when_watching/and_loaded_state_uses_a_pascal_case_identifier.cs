// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_loaded_state_uses_a_pascal_case_identifier : and_loaded_state_contains_cipher_shaped_plaintext
{
    protected override string IdentifierProperty => "Id";

    [Fact] void should_preserve_personal_cipher_shaped_plaintext() => _received[0]["name"]!.GetValue<string>().ShouldEqual(_plaintext);
    [Fact] void should_preserve_confidential_cipher_shaped_plaintext() => _received[0]["secret"]!.GetValue<string>().ShouldEqual(_plaintext);
    [Fact] void should_keep_personal_data_erased_on_update() => _received[1]["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_not_erase_namespace_confidentiality_on_update() => _received[1]["secret"]!.GetValue<string>().ShouldEqual(_plaintext);
}
