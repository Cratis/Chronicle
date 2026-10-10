// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// The exception that is thrown when a scope repair cannot durably record its audit event.
/// </summary>
public class ReopenStreamScopeAuditFailed() : Exception("The scope repair audit event could not be appended. The closure was not removed.");
