// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.when_round_tripping_security_ciphertext;

public class with_global_scope : given.a_security_schema
{
    protected override string MetadataType => "EncryptedGlobal";

    void Because() => _roundTripped = _converter.ToJsonObject(_converter.ToExpandoObject(_protectedContent, _schema), _schema);

    [Fact] void should_preserve_every_protected_value() => JsonNode.DeepEquals(_roundTripped, _protectedContent).ShouldBeTrue();
}
