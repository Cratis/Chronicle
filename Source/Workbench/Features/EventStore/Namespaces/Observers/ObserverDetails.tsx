// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { ObserverInformation } from 'Api/Observation/ObserverInformation';
import { ObserverOwner } from 'Api/Observation';
import { ConnectedClient, ConnectedClientsForObserver } from 'Api/Clients';
import strings from 'Strings';
import { getObserverRunningStateAsText } from './getObserverRunningStateAsText';
import { getObserverTypeAsText } from './getObserverTypeAsText';
import { getObserverOwnerAsText } from './getObserverOwnerAsText';
import css from './ObserverDetails.module.css';

/**
 * Props for the {@link ObserverDetails} component.
 */
export interface ObserverDetailsProps {
    /**
     * The observer to show details for.
     */
    observer: ObserverInformation;

    /**
     * The event store the observer belongs to.
     */
    eventStore: string;

    /**
     * The namespace within the event store the observer belongs to.
     */
    namespace: string;
}

export const ObserverDetails = ({ observer, eventStore, namespace }: ObserverDetailsProps) => {
    const isClientOwned = observer.owner === ObserverOwner.client;
    const [clients] = ConnectedClientsForObserver.when(isClientOwned).use({
        eventStore,
        namespace,
        observerId: observer.id,
        eventSequenceId: observer.eventSequenceId
    });

    const detailStrings = strings.eventStore.namespaces.observers.details;
    const clientStrings = detailStrings.connectedClients;

    const properties = [
        { label: detailStrings.sequence, value: observer.eventSequenceId },
        { label: detailStrings.observerType, value: getObserverTypeAsText(observer.type) },
        { label: detailStrings.owner, value: getObserverOwnerAsText(observer.owner) },
        { label: detailStrings.state, value: getObserverRunningStateAsText(observer.runningState) },
        { label: detailStrings.nextEventSequenceNumber, value: observer.nextEventSequenceNumber.toString() },
        { label: detailStrings.lastHandledEventSequenceNumber, value: observer.lastHandledEventSequenceNumber.toString() },
        { label: detailStrings.handledEventCount, value: observer.handledEventCount.toString() }
    ];

    const lastSeenColumn = (client: ConnectedClient) => <>{new Date(client.lastSeen).toLocaleString()}</>;

    return (
        <div className={css.observerDetails}>
            <h2 className={css.title}>{observer.id}</h2>
            <dl className={css.properties}>
                {properties.map(property => (
                    <div key={property.label} className={css.property}>
                        <dt className={css.propertyLabel}>{property.label}</dt>
                        <dd className={css.propertyValue}>{property.value}</dd>
                    </div>
                ))}
            </dl>

            {isClientOwned && (
                <div className={css.connectedClients}>
                    <h3 className={css.connectedClientsTitle}>{clientStrings.title}</h3>
                    <DataTable
                        value={clients.data ?? []}
                        dataKey='connectionId'
                        emptyMessage={clientStrings.empty}
                        size='small'>
                        <Column field='connectionId' header={clientStrings.columns.connectionId} />
                        <Column field='version' header={clientStrings.columns.version} />
                        <Column field='machineName' header={clientStrings.columns.machineName} />
                        <Column field='processId' header={clientStrings.columns.processId} />
                        <Column field='processPath' header={clientStrings.columns.processPath} />
                        <Column field='lastSeen' header={clientStrings.columns.lastSeen} body={lastSeenColumn} />
                    </DataTable>
                </div>
            )}
        </div>
    );
};
