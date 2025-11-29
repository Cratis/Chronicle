// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IDialogs } from '@cratis/arc.react.mvvm/dialogs';
import { injectable } from 'tsyringe';
import { AddEventTypeRequest, AddEventTypeResponse } from './AddEventType';

@injectable()
export class TypesViewModel {

    constructor(private readonly _dialogs: IDialogs) {
    }

    async addEventType() {
        const [, response]= await this._dialogs.show<AddEventTypeRequest, AddEventTypeResponse>(new AddEventTypeRequest());
        console.log(response!.name);

        // Execute AddEventType command
    }
}
