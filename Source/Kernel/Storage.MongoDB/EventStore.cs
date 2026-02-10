// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an event store.
/// </summary>
/// <param name="Name">Name of event store.</param>
/// <param name="DomainSpecification">Optional domain specification describing the purpose and context.</param>
public record EventStore(EventStoreName Name, DomainSpecification? DomainSpecification = null);
