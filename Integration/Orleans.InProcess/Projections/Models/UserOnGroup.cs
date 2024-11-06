// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.Models;

public class UserOnGroup()
{
    public UserId UserId { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Onboarded { get; set; } = false;
}
