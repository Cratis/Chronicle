// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Concepts;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ReadModels;

public class UserOnGroup()
{
    public UserId UserId { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public bool Onboarded { get; set; }
}
