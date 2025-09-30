// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IDialogs } from '@cratis/applications.react.mvvm/dialogs';
import { inject, injectable } from 'tsyringe';
import { AddWebhookRequest, AddWebhookResponse } from './AddWebhook';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { RegisterWebhook } from 'Api/Observation/Webhooks/RegisterWebhook';
import {AllEventTypes} from "Api/EventTypes";

@injectable()
export class WebhooksViewModel {

    constructor(
        @inject('params') private readonly _params: EventStoreAndNamespaceParams,
        private readonly _dialogs: IDialogs) {
    }

    async addWebhook() {
        const query = new AllEventTypes();
        const res = await query.perform({ eventStore: this._params.eventStore!});
        
        const request = new AddWebhookRequest();
        request.eventTypes = res.data;
        const [, response] = await this._dialogs.show<AddWebhookRequest, AddWebhookResponse>(request);
        if (!response!.create) return;

        const command = new RegisterWebhook();
        command.eventStore = this._params.eventStore!;
        command.eventSequenceId = response!.eventSequence;
        command.target = response!.target!;
        command.eventTypes = response!.eventTypes;
        debugger;
        await command.execute();
    }
}
