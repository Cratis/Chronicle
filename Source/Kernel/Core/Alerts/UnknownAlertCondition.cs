// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// The exception that is thrown when settings are asked for an alert condition the kernel does not know.
/// </summary>
/// <param name="kind">The <see cref="AlertConditionKind"/> that is not known.</param>
public class UnknownAlertCondition(AlertConditionKind kind) : Exception($"The alert condition '{kind}' is not known");
