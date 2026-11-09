// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_members_report_confirmed_erasure;

public class without_fence_guarantees : conformance
{
    protected override EncryptionKeyFenceGuarantees MemberGuarantees => EncryptionKeyFenceGuarantees.None;

    protected override bool ExpectedConfirmation => false;
}
