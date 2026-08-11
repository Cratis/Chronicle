// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager;

/// <summary>
/// The exception that is thrown by a substituted key store standing in for one that cannot be reached.
/// </summary>
public class StoreUnreachable() : Exception("The encryption key store cannot be reached.");
