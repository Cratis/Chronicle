// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The exception that is thrown when a federation registry event could not be appended to the system event log.
/// </summary>
/// <param name="id">The binding or link id.</param>
/// <param name="errors">The append errors.</param>
public class FederationEventAppendFailed(string id, string errors) : Exception($"The federation event for '{id}' could not be appended: {errors}");
