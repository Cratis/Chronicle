// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Events.AccountHolders;

[EventType("ccb6afd1-e47e-45e0-8d37-6d2a8d071344")]
public record AccountHolderAddressChanged(string AddressLine, string City, string PostalCode, string Country);
