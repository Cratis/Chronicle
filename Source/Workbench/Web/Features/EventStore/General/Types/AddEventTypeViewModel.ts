// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { AddEventTypeResponse, type AddEventTypeProps } from './AddEventType';

@injectable()
export class AddEventTypeViewModel {

    constructor(@inject('props') private readonly _props: AddEventTypeProps) {
    }

    name: string = '';

    proceed() {
        this._props.resolver(new AddEventTypeResponse(this.name));
    }

    cancel() {
        this._props.resolver(new AddEventTypeResponse(''));
    }
}
