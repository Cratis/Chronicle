// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when local trigger discovery or kernel registration cannot establish a valid trigger contract.
/// </summary>
/// <param name="reason">The invalid declaration or registration failure.</param>
public class TriggerNotValid(string reason) : Exception(reason);
