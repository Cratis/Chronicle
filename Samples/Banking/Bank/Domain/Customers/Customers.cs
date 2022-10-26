// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Customers;

namespace Domain.Customers;

[Route("/api/customers")]
public class Customers : Controller
{
    readonly IEventLog _eventLog;

    public Customers(IEventLog eventLog) => _eventLog = eventLog;

    [HttpPost]
    public Task RegisterCustomer(RegisterCustomer command) => _eventLog.Append(command.CustomerId, new CustomerRegistered(command.FirstName, command.LastName, command.SocialSecurityNumber));
}
