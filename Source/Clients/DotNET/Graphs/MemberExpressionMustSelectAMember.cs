// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a query clause is given an expression that does not select a member of the view.
/// </summary>
/// <param name="expression">The expression.</param>
public class MemberExpressionMustSelectAMember(string expression)
    : Exception($"The expression '{expression}' must select a member of the view, for example _ => _.Name.");
