// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from 'Infrastructure';
import { NullObservableQueryConnection } from './NullObservableQueryConnection';
import { ObservableQuerySubscription, OnNextResult, QueryResult } from 'Infrastructure/queries';
import { container } from 'tsyringe';
import { AllObservers } from 'API/events/store/observers/AllObservers';
import { ObserverState } from 'API/events/store/observers/ObserverState';
import observers from './Observers.json';


function registerFakeQuery<TDataType>(queryType: Constructor, itemConstructor: Constructor, data: any) {
    const query = new queryType() as any;

    const response = {
        "data": data,
        "isSuccess": true,
        "isAuthorized": true,
        "isValid": true,
        "hasExceptions": false,
        "validationResults": [],
        "exceptionMessages": [],
        "exceptionStackTrace": ""
    };

    query.subscribe = (callback: OnNextResult<QueryResult<TDataType>>, args?: any | undefined) => {
        const result = new QueryResult<TDataType>(response, itemConstructor, true);
        callback(result);
        return new ObservableQuerySubscription(new NullObservableQueryConnection());
    };
    container.registerInstance(queryType, query);
}


export class FakeData {
    static initialize() {
        registerFakeQuery(AllObservers, ObserverState, observers);
    }
}

