// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance.Events;
using Cratis.Compliance.GDPR;

namespace Cratis.Compliance.Reactions.GDPR
{
    [Observer("f0ef19ad-2ea4-4b8e-b93a-a23d176abcfe")]
    public class PersonalInformation
    {
        readonly IPIIManager _piiManager;

        public PersonalInformation(IPIIManager piiManager)
        {
            _piiManager = piiManager;
        }

        public Task PersonalInformationDeleted(PersonalInformationForPersonDeleted _, EventContext context) => _piiManager.DeleteEncryptionKeyFor(context.EventSourceId.Value);
    }
}
