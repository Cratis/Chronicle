// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a composite key cannot be looked up because the read model still holds rows stored under the
/// legacy delimiter-joined key, and this key is ambiguous in that form so it cannot be told apart from another key.
/// </summary>
/// <param name="readModel">The identifier of the read model.</param>
/// <param name="key">The encoded key that was looked up.</param>
public class LegacyCompositeKeyRequiresReplay(ReadModelIdentifier readModel, string key)
    : Exception($"The composite key '{key}' of read model '{readModel}' cannot be looked up: the read model still has rows stored under the legacy underscore-joined key and this key is ambiguous in that form. Replay the read model to rewrite its keys.");
