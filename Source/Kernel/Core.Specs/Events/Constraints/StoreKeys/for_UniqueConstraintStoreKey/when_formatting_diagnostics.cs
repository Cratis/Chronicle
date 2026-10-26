// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintStoreKey;

public class when_formatting_diagnostics : given.secret_material
{
    string _direct;
    string _record;

    void Because()
    {
        _direct = _material.ToString();
        _record = _containing.ToString();
    }

    [Fact] void should_redact_direct_diagnostics() => _direct.ShouldEqual("[REDACTED]");
    [Fact] void should_redact_containing_record_diagnostics() => _record.ShouldEqual($"UniqueConstraintEncodingKey {{ KeyId = {_containing.KeyId}, Material = [REDACTED] }}");
}
