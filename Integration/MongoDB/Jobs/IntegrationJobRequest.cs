// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.MongoDB.Integration.Jobs;

public record IntegrationJobRequest(string Name, int Count, DateTimeOffset Timestamp) : IJobRequest;
