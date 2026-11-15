// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_members_report_confirmed_erasure;

public class with_full_guarantees : conformance
{
    protected override EncryptionKeyFenceGuarantees MemberGuarantees => EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave;

    protected override bool ExpectedConfirmation => true;
}
