// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from '@cratis/components/DataTables';
import { DataTableCore } from '@cratis/components/DataTables';
import { Tabs, TabPanel } from 'Components/Tabs';
import { ObserverInformation } from 'Features/Observation';
import { ObserverOwner } from 'Features/Contracts/Observation';
import { ConnectedClientDetails, ConnectedClientsForObserver } from 'Features/Clients';
import strings from 'Strings';
import { getObserverRunningStateAsText } from './getObserverRunningStateAsText';
import { getObserverTypeAsText } from './getObserverTypeAsText';
import { getObserverOwnerAsText } from './getObserverOwnerAsText';
import { ObserverEventTypes } from './ObserverEventTypes';
import { type ObserverLagDisplay, formatAge, formatPosition } from './observerLagPresentation';
import { describeObserverLagState } from './describeObserverLag';
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

    /**
     * The sampled progress of the observer as it stands now, or undefined when none can be shown.
     */
    lag?: ObserverLagDisplay;

    /**
     * Whether a read of the sampled progress has completed, so that no lag means unavailable rather than not yet read.
     */
    isLagLoaded?: boolean;
}

export const ObserverDetails = ({ observer, eventStore, namespace, lag, isLagLoaded = false }: ObserverDetailsProps) => {
    const isClientOwned = observer.owner === ObserverOwner.client;
    const [clients] = ConnectedClientsForObserver.when(isClientOwned).use({
        eventStore,
        namespace,
        observerId: observer.id,
        eventSequenceId: observer.eventSequenceId
    });

    const detailStrings = strings.eventStore.namespaces.observers.details;
    const clientStrings = detailStrings.connectedClients;
    const sampledStrings = detailStrings.sampled;
    const lagStrings = strings.eventStore.namespaces.observers.lag;

    const properties = [
        { label: detailStrings.sequence, value: observer.eventSequenceId },
        { label: detailStrings.observerType, value: getObserverTypeAsText(observer.type) },
        { label: detailStrings.owner, value: getObserverOwnerAsText(observer.owner) },
        { label: detailStrings.state, value: getObserverRunningStateAsText(observer.runningState) },
        { label: detailStrings.nextEventSequenceNumber, value: formatPosition(observer.nextEventSequenceNumber) ?? lagStrings.notShown },
        { label: detailStrings.lastHandledEventSequenceNumber, value: formatPosition(observer.lastHandledEventSequenceNumber) ?? lagStrings.notShown },
        { label: detailStrings.handledEventCount, value: observer.handledEventCount.toString() }
    ];

    const unavailable = isLagLoaded ? lagStrings.unavailable : lagStrings.loading;
    const sampledProperties = [
        { label: sampledStrings.state, value: lag ? describeObserverLagState(lag.state) : unavailable },
        { label: sampledStrings.distance, value: lag?.distance ?? lagStrings.notShown },
        { label: sampledStrings.age, value: lag?.ageSeconds === undefined ? lagStrings.notShown : formatAge(lag.ageSeconds) },
        { label: sampledStrings.tail, value: lag?.tail ?? lagStrings.notShown },
        { label: sampledStrings.next, value: lag?.next ?? lagStrings.notShown },
        { label: sampledStrings.lastHandled, value: lag?.lastHandled ?? lagStrings.notShown },
        { label: sampledStrings.relevantPending, value: lag?.relevantPending ?? lagStrings.notShown }
    ];

    const lastSeenColumn = (client: ConnectedClientDetails) => <>{new Date(client.lastSeen).toLocaleString()}</>;

    return (
        <div className={css.observerDetails}>
            <h2 className={css.title}>{observer.id}</h2>
            <Tabs className={css.tabs} panelContainerClassName={css.tabPanelContainer}>
                <TabPanel header={detailStrings.tabs.summary}>
                    <dl className={css.properties}>
                        {properties.map(property => (
                            <div key={property.label} className={css.property}>
                                <dt className={css.propertyLabel}>{property.label}</dt>
                                <dd className={css.propertyValue}>{property.value}</dd>
                            </div>
                        ))}
                    </dl>

                    <h3 className={css.connectedClientsTitle}>{sampledStrings.title}</h3>
                    <dl className={css.properties}>
                        {sampledProperties.map(property => (
                            <div key={property.label} className={css.property}>
                                <dt className={css.propertyLabel}>{property.label}</dt>
                                <dd className={css.propertyValue}>{property.value}</dd>
                            </div>
                        ))}
                    </dl>

                    {isClientOwned && (
                        <div className={css.connectedClients}>
                            <h3 className={css.connectedClientsTitle}>{clientStrings.title}</h3>
                            <DataTableCore
                                data={clients.data ?? []}
                                dataKey='connectionId'
                                emptyMessage={clientStrings.empty}>
                                <Column field='connectionId' header={clientStrings.columns.connectionId} />
                                <Column field='clientType' header={clientStrings.columns.clientType} />
                                <Column field='version' header={clientStrings.columns.version} />
                                <Column field='machineName' header={clientStrings.columns.machineName} />
                                <Column field='processId' header={clientStrings.columns.processId} />
                                <Column field='processPath' header={clientStrings.columns.processPath} />
                                <Column field='lastSeen' header={clientStrings.columns.lastSeen} body={lastSeenColumn} />
                            </DataTableCore>
                        </div>
                    )}
                </TabPanel>
                <TabPanel header={detailStrings.tabs.eventTypes}>
                    <div className={css.eventTypesPanel}>
                        <ObserverEventTypes observer={observer} />
                    </div>
                </TabPanel>
            </Tabs>
        </div>
    );
};
