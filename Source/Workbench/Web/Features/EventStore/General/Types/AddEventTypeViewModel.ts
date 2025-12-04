// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { AddEventTypeResponse, type AddEventTypeRequest } from './AddEventType';
import { DialogContextContent, DialogResult } from '@cratis/arc.react/dialogs';

@injectable()
export class AddEventTypeViewModel {

    constructor(
        private readonly _dialogContext: DialogContextContent<AddEventTypeRequest, AddEventTypeResponse>) {
    }

    name: string = '';

    proceed() {
        this._dialogContext.closeDialog(DialogResult.Ok, new AddEventTypeResponse(this.name));
    }

    cancel() {
        this._dialogContext.closeDialog(DialogResult.Cancelled, new AddEventTypeResponse(''));
    }
}
