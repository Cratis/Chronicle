// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a raw Chronicle read occurs without an explicit allowance.
/// </summary>
/// <param name="operation">The public read operation.</param>
public class ReadRefusedWhileReadsAreProtected(string operation) : Exception($"The command called {operation} while read protection is On. Take a read model as a parameter, use IDecisionReads.Get in Provide, or mark the method [Unprotected(\"why\")].");
