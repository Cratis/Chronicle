// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs;

public record AnotherJobRequest(string Id, string Description) : IJobRequest;
