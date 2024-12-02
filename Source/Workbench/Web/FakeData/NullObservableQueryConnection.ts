// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryConnection } from '@cratis/applications/queries';


/* eslint-disable @typescript-eslint/no-empty-function */

export class NullObservableQueryConnection<TDataType> implements IObservableQueryConnection<TDataType> {
    connect() {

    }
    disconnect() {

    }
}
