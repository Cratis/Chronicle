// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { TabPanel, TabView } from 'primereact/tabview';
import { Button } from 'primereact/button';
import strings from 'Strings';
import { Page } from 'Components/Common/Page';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { AllEventTypes } from 'Api/EventTypes/AllEventTypes';
import { AllSequenceQueries } from 'Api/SequenceQueries/AllSequenceQueries';
import { getDistinctEventTypeOptions } from './getDistinctEventTypeOptions';
import { QueryEditor } from './QueryEditor/QueryEditor';
import { SavedQueries } from './QueryEditor/SavedQueries';
import { useOpenQueries } from './QueryEditor/useOpenQueries';
import './Sequences.css';

/**
 * The event sequence workspace: a list of the queries the user has saved, and the ones they
 * currently have open as tabs.
 * @returns The rendered page.
 */
export const Sequences = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const eventStore = params.eventStore!;
    const namespace = params.namespace!;
    const sequenceStrings = strings.eventStore.namespaces.sequences;

    const [eventTypes] = AllEventTypes.use({ eventStore });
    const eventTypeIds = useMemo(
        () => getDistinctEventTypeOptions(eventTypes.data).map(option => option.value),
        [eventTypes.data]
    );

    const [savedQueries] = AllSequenceQueries.use({ eventStore });
    const queriesForNamespace = useMemo(
        () => savedQueries.data.filter(query => query.namespace === namespace),
        [savedQueries.data, namespace]
    );

    const { open, activeIndex, setActiveIndex, update, add, close, openSaved } =
        useOpenQueries(queriesForNamespace, namespace, sequenceStrings.newQuery);

    return (
        <Page title={sequenceStrings.title}>
            <div className='sequences'>
                <SavedQueries
                    queries={queriesForNamespace}
                    eventStore={eventStore}
                    openIds={open.map(query => query.id)}
                    onOpen={openSaved} />

                <div className='sequences__queries'>
                    <div className='sequences__tabs'>
                        <TabView
                            className='sequences__tabview'
                            activeIndex={activeIndex}
                            onTabChange={event => setActiveIndex(event.index)}
                            onTabClose={event => close(event.index)}>
                            {open.map((query, index) => (
                                <TabPanel key={query.id} header={query.name || sequenceStrings.newQuery} closable>
                                    <QueryEditor
                                        state={query}
                                        eventStore={eventStore}
                                        eventTypeIds={eventTypeIds}
                                        onChange={state => update(index, state)} />
                                </TabPanel>
                            ))}
                        </TabView>

                        <Button
                            className='sequences__add'
                            icon='pi pi-plus'
                            text
                            aria-label={sequenceStrings.actions.newQuery}
                            tooltip={sequenceStrings.actions.newQuery}
                            onClick={add} />
                    </div>
                </div>
            </div>
        </Page>
    );
};
