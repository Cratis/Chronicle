// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyFenceRollout;

public class on_sqlite() : when_rolling_out(for_EncryptionKeyStorage.given.a_key_store.Sqlite());
