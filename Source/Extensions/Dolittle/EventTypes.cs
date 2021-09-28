// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Schema;

namespace Cratis.Extensions.Dolittle
{
    [Route("/api/events/types")]
    public class EventTypes : Controller
    {
        [HttpGet]
        public Task<IEnumerable<EventType>> AllEvenTtypes()
        {
            return Task.FromResult(new[] {
                new EventType("e871c9c9-49be-4881-b4ad-9f3b244ba688","DebitAccountOpened"),
                new EventType("544c04a1-ee31-4f81-a716-71c729d2aaa7","DepositToDebitAccountPerformed")
            }.AsEnumerable());
        }

        [HttpGet("schemas")]
        public Task<JSchema> AllEventTypeSchemas()
        {
            return Task.FromResult(new JSchema());
        }
    }
}
