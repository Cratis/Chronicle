// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {DialogResult, useDialogContext} from '@cratis/applications.react/dialogs';
import {Button} from 'primereact/button';
import {Dialog} from 'primereact/dialog';
import {useState} from 'react';
import strings from 'Strings';
import {AuthenticationType, WebhookTarget} from "Api/Observation/Webhooks";
import {Dropdown} from "primereact/dropdown";
import {EventType} from "Api/Events";
import {InputText} from "primereact/inputtext";
import {MultiSelect} from "primereact/multiselect";

export class AddWebhookRequest {
    eventTypes!: EventType[];
}

export class AddWebhookResponse {
    static readonly Canceled = new AddWebhookResponse(false, '', [], null);
    static Ok = (eventSequence: string, eventTypes: EventType[], target: WebhookTarget) => new AddWebhookResponse(true, eventSequence, eventTypes, target);

    constructor(readonly create: boolean, readonly eventSequence: string, readonly eventTypes: EventType[], readonly target: WebhookTarget | null) {
    }
}

export const AddWebhook = () => {
    const [eventSequence, setEventSequence] = useState('event-log');
    const [selectedEventTypes, setSelectedEventTypes] = useState<EventType[]>([]);
    const [url, setUrl] = useState('');
    const { closeDialog, request } = useDialogContext<AddWebhookRequest, AddWebhookResponse>();

    return (
        <Dialog header={strings.eventStore.general.webhooks.dialogs.addWebhook.title} visible={true} style={{ width: '20vw' }} modal onHide={() => closeDialog(DialogResult.Cancelled, AddWebhookResponse.Canceled)}>
            <div className="card flex flex-column gap-3">
                <div className="p-inputgroup flex-1 flex flex-column gap-2">
                    <Dropdown placeholder={strings.eventStore.general.webhooks.dialogs.addWebhook.eventSequence} value={eventSequence}
                              onChange={e => setEventSequence(e.value)} options={['event-log', 'system']} optionLabel='event-sequence' />
                </div>
            </div>
            <div className="card flex flex-column gap-3">
                <div className="p-inputgroup flex-1 flex flex-column gap-2">
                    
                    <MultiSelect placeholder={strings.eventStore.general.webhooks.dialogs.addWebhook.eventTypes} value={selectedEventTypes}
                              onChange={e => setSelectedEventTypes(e.target.value)} options={request.eventTypes} optionLabel='id' />

                </div>
            </div>
            <div className="card flex flex-column gap-3">
                <div className="p-inputgroup flex-1 flex flex-column gap-2">
                    <InputText placeholder={strings.eventStore.general.webhooks.dialogs.addWebhook.target.url} value={url} onChange={e => setUrl(e.target.value)} />
                </div>
            </div>

            <div className="card flex flex-wrap justify-content-center gap-3 mt-8">
                <Button label={strings.general.buttons.ok} icon="pi pi-check" onClick={() => closeDialog(DialogResult.Ok, AddWebhookResponse.Ok(
                    eventSequence,
                    selectedEventTypes.length === 0
                        ? []
                        : request.eventTypes.filter(requestEventType => selectedEventTypes.map(selected => selected.id).includes(requestEventType.id)),
                    {url, authentication: AuthenticationType.none, bearerToken: '', headers: [], password: '', username: ''}
                ))} autoFocus />
                <Button label={strings.general.buttons.cancel} icon="pi pi-times" severity='secondary' onClick={() => closeDialog(DialogResult.Cancelled, AddWebhookResponse.Canceled)} />
            </div>
        </Dialog>
    );
};
