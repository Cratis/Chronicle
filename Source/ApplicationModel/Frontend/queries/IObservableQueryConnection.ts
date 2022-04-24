// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataReceived } from './ObservableQueryConnection';

/**
 * Defines a connection for observable queries.
 */
export interface IObservableQueryConnection<TDataType> {
    /**
     * Connect to a specific route.
     * @param {DataReceived<TDataType> dataReceived Callback that will receive the data.
     */
    connect(dataReceived: DataReceived<TDataType>);

    /**
     * Disconnect the connection.
     */
    disconnect();
}
