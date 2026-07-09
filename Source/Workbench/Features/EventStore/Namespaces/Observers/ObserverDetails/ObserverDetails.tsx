// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TabView, TabPanel } from 'primereact/tabview';
import { ObserverInformation } from 'Api/Observation';
import { ObserverSummary } from './ObserverSummary';
import { ObserverEventTypes } from './ObserverEventTypes';
import strings from 'Strings';
import './ObserverDetails.css';

/**
 * Props for {@link ObserverDetails}.
 */
export interface ObserverDetailsProps {
    /**
     * The observer to render details for.
     */
    observer: ObserverInformation;
}

/**
 * Composes a tabbed detail view of a single observer with a summary panel and the list of event types consumed.
 *
 * @param props - The {@link ObserverDetailsProps}.
 */
export const ObserverDetails = ({ observer }: ObserverDetailsProps) => {
    const tabStrings = strings.eventStore.namespaces.observers.details.tabs;

    return (
        <div className='observer-details'>
            <TabView className='observer-details__tabs'
                panelContainerClassName='observer-details__tab-content'>
                <TabPanel header={tabStrings.summary}
                    contentClassName='observer-details__tab-panel-content'>
                    <ObserverSummary observer={observer} />
                </TabPanel>
                <TabPanel header={tabStrings.eventTypes}
                    contentClassName='observer-details__tab-panel-content'>
                    <ObserverEventTypes observer={observer} />
                </TabPanel>
            </TabView>
        </div>
    );
};
