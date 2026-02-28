// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryConnection } from '@cratis/arc/queries';


/* eslint-disable @typescript-eslint/no-empty-function */

export class NullObservableQueryConnection<TDataType> implements IObservableQueryConnection<TDataType> {
    // Provide the latency metrics expected by the interface
    public lastPingLatency = 0;
    public averageLatency = 0;

    connect() {

    }
    disconnect() {

    }
}
