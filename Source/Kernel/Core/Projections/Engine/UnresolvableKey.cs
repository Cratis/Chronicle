// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents a key resolution that permanently failed — all resolution strategies were exhausted
/// and no parent was found. No deferred future is created; the event is silently skipped for the
/// affected child projection.
/// </summary>
public record UnresolvableKey : KeyResolverResult;
