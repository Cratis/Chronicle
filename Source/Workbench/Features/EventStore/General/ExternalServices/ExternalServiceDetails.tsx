// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IDetailsComponentProps } from '@cratis/components/DataPage';
import { ExternalServiceDefinition, ExternalServiceEndpointType } from 'Api/ExternalServices';
import { getEndpointTypeString } from './getEndpointTypeString';
import { getAuthorizationTypeString } from './getAuthorizationTypeString';
import strings from 'Strings';

export const ExternalServiceDetails = (props: IDetailsComponentProps<ExternalServiceDefinition>) => {
    const endpointTypeName = getEndpointTypeString(props.item.endpointType);
    const isHttp = props.item.endpointType === ExternalServiceEndpointType.http;

    return (
        <div className="external-service-details p-4" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <h3 className="mb-4">{props.item.name}</h3>

            <div className="field mb-3">
                <label className="font-bold block mb-2">{strings.eventStore.general.externalServices.columns.endpointType}</label>
                <div className="text-sm">{endpointTypeName}</div>
            </div>

            {isHttp && (
                <>
                    <div className="field mb-3">
                        <label className="font-bold block mb-2">{strings.eventStore.general.externalServices.columns.url}</label>
                        <div className="text-sm">{props.item.url}</div>
                    </div>

                    <div className="field mb-3">
                        <label className="font-bold block mb-2">{strings.eventStore.general.externalServices.columns.authorization}</label>
                        <div className="text-sm">{getAuthorizationTypeString(props.item.authorizationType)}</div>
                    </div>

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
                </>
            )}

            {!isHttp && (
                <>
                    <div className="field mb-3">
                        <label className="font-bold block mb-2">{strings.eventStore.general.externalServices.columns.host}</label>
                        <div className="text-sm">{props.item.host}</div>
                    </div>

                    <div className="field mb-3">
                        <label className="font-bold block mb-2">{strings.eventStore.general.externalServices.columns.port}</label>
                        <div className="text-sm">{props.item.port}</div>
                    </div>

                    <div className="field mb-3">
                        <label className="font-bold block mb-2">{strings.eventStore.general.externalServices.columns.database}</label>
                        <div className="text-sm">{props.item.database}</div>
                    </div>

                    <div className="field mb-3">
                        <label className="font-bold block mb-2">{strings.eventStore.general.externalServices.columns.username}</label>
                        <div className="text-sm">{props.item.username}</div>
                    </div>

                    {Object.keys(props.item.options || {}).length > 0 && (
                        <div className="field mb-3">
                            <label className="font-bold block mb-2">Options</label>
                            <div className="text-sm">
                                <ul>
                                    {Object.entries(props.item.options).map(([key, value]) => (
                                        <li key={key}><strong>{key}:</strong> {String(value)}</li>
                                    ))}
                                </ul>
                            </div>
                        </div>
                    )}
                </>
            )}
        </div>
    );
};
