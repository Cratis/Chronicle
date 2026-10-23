// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.given;

/// <summary>
/// Prevents process-wide opt-out changes while another spec constructs stores.
/// </summary>
[CollectionDefinition(nameof(schema_sharing_collection), DisableParallelization = true)]
public class schema_sharing_collection;
