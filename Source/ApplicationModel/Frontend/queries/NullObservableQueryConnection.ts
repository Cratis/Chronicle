// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataReceived } from './ObservableQueryConnection';
import { IObservableQueryConnection } from './IObservableQueryConnection';
import { QueryResult } from './QueryResult';

/**
 * Represents a {@link IObservableQueryConnection} when for instance one can't establish a connection.
 */

export class NullObservableQueryConnection<TDataType> implements IObservableQueryConnection<TDataType> {
    /**
     * Initializes a new instance of the {@link NullObservableQueryConnection} class.
     * @param {TDataType} defaultValue The default value to serve.
     */
    constructor(readonly defaultValue: TDataType) {
    }

    /** @inheritdoc */
    connect(dataReceived: DataReceived<TDataType>) {
        dataReceived(new QueryResult(this.defaultValue, true));
    }

    /** @inheritdoc */
    disconnect() {
    }
}
