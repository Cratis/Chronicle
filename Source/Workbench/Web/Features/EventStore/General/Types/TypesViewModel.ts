// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IDialogs } from '@cratis/arc.react.mvvm/dialogs';
import { injectable } from 'tsyringe';
import { AddEventTypeRequest, AddEventTypeResponse } from './AddEventType';
import { CreateEventType } from 'Api/EventTypes';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { ICommandManager } from '@cratis/arc.react/commands';
import { useParams } from 'react-router-dom';

@injectable()
export class TypesViewModel {

    constructor(
        private readonly _dialogs: IDialogs,
        private readonly _commandManager: ICommandManager) {
    }

    async addEventType(eventStore: string) {
        const [result, response] = await this._dialogs.show<AddEventTypeRequest, AddEventTypeResponse>(new AddEventTypeRequest());
        
        if (result === DialogResult.Ok && response && response.name) {
            await this._commandManager.perform(CreateEventType, { name: response.name }, { eventStore });
            // Refresh the list by triggering a re-render or refetch
            window.location.reload(); // Simple approach - could be improved
        }
    }
}
