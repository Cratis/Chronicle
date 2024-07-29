// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Defines an interface for an aggregate root in Orleans.
/// </summary>
public interface IAggregateRoot : Chronicle.Aggregates.IAggregateRoot, IGrainWithStringKey;
