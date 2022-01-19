// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryConnection } from './ObservableQueryConnection';


/**
 * Represents a subscription for an observable query.
 */
export class ObservableQuerySubscription<TDataType> {

    /**
     * Initializes a new instance of the {@link ObservableQuerySubscription} class.
     * @param {ObservableQueryConnection<TDataType> _connection The connection to use.
     */
    constructor(private _connection: ObservableQueryConnection<TDataType>) {
    }

    /**
     * Unsubscribe subscription.
     */
    unsubscribe() {
        this._connection.disconnect();
        this._connection = undefined!;
    }
}
