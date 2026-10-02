// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// The exception that is thrown when incident query arguments cannot describe a valid scope or continuation.
/// </summary>
/// <param name="reason">The invalid argument reason.</param>
public class InvalidAlertIncidentQuery(string reason) : Exception(reason);
