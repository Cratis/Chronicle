// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance.Grains;
using Orleans;

namespace Cratis.Compliance.Domain.GDPR
{
    [Route("/api/compliance/gdpr/pii")]
    public class PII : Controller
    {
        readonly IClusterClient _clusterClient;

        public PII(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        [HttpPost]
        public async Task CreateAndRegisterKeyFor([FromBody] CreateAndRegisterKeyFor command)
        {
            var piiManager = _clusterClient.GetGrain<IPIIManager>(Guid.Empty);
            await piiManager.CreateAndRegisterKeyFor(command.Identifier);
        }
    }
}
