// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IDetailsComponentProps } from 'Components';
import { WebhookDefinition } from 'Api/Webhooks';
import { getAuthorizationTypeString } from './getAuthorizationTypeString';
import strings from 'Strings';

export const WebhookDetails = (props: IDetailsComponentProps<WebhookDefinition>) => {
    const authTypeName = getAuthorizationTypeString(props.item.authorizationType);

    return (
        <div className="webhook-details p-4" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <h3 className="mb-4">{props.item.name}</h3>

            <div className="field mb-3">
                <label className="font-bold block mb-2">{strings.eventStore.general.webhooks.columns.url}</label>
                <div className="text-sm">{props.item.url}</div>
            </div>

            <div className="field mb-3">
                <label className="font-bold block mb-2">{strings.eventStore.general.webhooks.columns.authorization}</label>
                <div className="text-sm">{authTypeName}</div>
            </div>

            <div className="field mb-3">
                <label className="font-bold block mb-2">{strings.eventStore.general.webhooks.columns.active}</label>
                <div className="text-sm">{props.item.isActive ? 'Yes' : 'No'}</div>
            </div>

            <div className="field mb-3">
                <label className="font-bold block mb-2">{strings.eventStore.general.webhooks.columns.replayable}</label>
                <div className="text-sm">{props.item.isReplayable ? 'Yes' : 'No'}</div>
            </div>

            {Object.keys(props.item.eventTypes || {}).length > 0 && (
                <div className="field mb-3">
                    <label className="font-bold block mb-2">Event Types</label>
                    <div className="text-sm">
                        <ul>
                            {Object.keys(props.item.eventTypes).map((eventType) => (
                                <li key={eventType}>{eventType}</li>
                            ))}
                        </ul>
                    </div>
                </div>
            )}

            {Object.keys(props.item.headers || {}).length > 0 && (
                <div className="field mb-3">
                    <label className="font-bold block mb-2">Headers</label>
                    <div className="text-sm">
                        <ul>
                            {Object.entries(props.item.headers).map(([key, value]) => (
                                <li key={key}><strong>{key}:</strong> {String(value)}</li>
                            ))}
                        </ul>
                    </div>
                </div>
            )}
        </div>
    );
};
