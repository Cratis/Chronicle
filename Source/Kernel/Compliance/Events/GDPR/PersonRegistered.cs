// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance.Concepts.PersonalInformation;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Compliance.Events;

[EventType("12b88453-12c9-4f99-a60c-abb21282aede")]
public record PersonRegistered(FirstName FirstName, LastName LastName, SocialSecurityNumber SocialSecurityNumber);
