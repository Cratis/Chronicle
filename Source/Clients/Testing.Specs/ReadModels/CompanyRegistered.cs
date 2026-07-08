// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event registering a company; lives on the company's own string-org-number event source and is joined
/// into engagements.
/// </summary>
/// <param name="Name">The company name.</param>
[EventType]
public record CompanyRegistered(string Name);
