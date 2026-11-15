// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>The explicit issuer-qualified principal used by grant administration.</summary>
/// <param name="Issuer">The exact issuer.</param>
/// <param name="Subject">The exact subject or client identifier.</param>
/// <param name="IsClient">Whether the subject identifies an external client.</param>
public record EventStoreAccessHolder(string Issuer, string Subject, bool IsClient);
