// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;

namespace Cratis.Chronicle.MongoDB.Integration.Jobs;

#pragma warning disable SA1649 // File name should match first type name
public interface IIntegrationJob;

[JobType(nameof(IntegrationJob))]
public class IntegrationJob : IIntegrationJob;
