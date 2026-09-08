// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';
import { IDetailsComponentProps } from '@cratis/components/DataPage';
import { SchemaEditor } from '@cratis/components/SchemaEditor';
import type { JsonSchema } from '@cratis/components/types';
import { AllTypeFormats } from 'Features/Schemas';
import { EventTypeSource } from 'Features/Contracts/Events';
import { EventTypeDetails } from 'Features/EventTypes';
import { RegisterEventTypes } from 'Features/EventTypes';
import { AllEventTypeGenerations } from 'Features/EventTypes';
import { Dropdown } from '@cratis/components/Dropdown';
import { Tabs, TabPanel } from 'Components/Tabs';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { ObserversForEventType } from './ObserversForEventType';
import strings from 'Strings';

interface GenerationOption {
    label: string;
    value: number;
    registration: EventTypeDetails;
}

export const TypeDetails = (props: IDetailsComponentProps<EventTypeDetails>) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [schema, setSchema] = useState<JsonSchema>(() => JSON.parse(props.item.schema));
    const [selectedGeneration, setSelectedGeneration] = useState<number>(props.item.type.generation);
    const [currentRegistration, setCurrentRegistration] = useState<EventTypeDetails>(props.item);
    const [register] = RegisterEventTypes.use();
    const [generationsQuery, performGenerationsQuery] = AllEventTypeGenerations.use({
        eventStore: params.eventStore!,
        eventTypeId: props.item.type.id
    });

    // Reload generations when event type changes
    useEffect(() => {
        setSelectedGeneration(props.item.type.generation);
        setCurrentRegistration(props.item);
        setSchema(JSON.parse(props.item.schema));
        performGenerationsQuery({ eventStore: params.eventStore!, eventTypeId: props.item.type.id });
    }, [props.item.type.id, params.eventStore]);

    // Build generation dropdown options
    const generationOptions: GenerationOption[] = generationsQuery.data?.map(reg => ({
        label: `${strings.eventStore.general.eventTypes.columns.generation} ${reg.type.generation}`,
        value: reg.type.generation,
        registration: reg
    })) ?? [{ label: `${strings.eventStore.general.eventTypes.columns.generation} ${props.item.type.generation}`, value: props.item.type.generation, registration: props.item }];

    const hasMultipleGenerations = generationOptions.length > 1;

    const handleGenerationChange = (generation: number) => {
        setSelectedGeneration(generation);
        const selected = generationOptions.find(option => option.value === generation);
        if (selected) {
            setCurrentRegistration(selected.registration);
            setSchema(JSON.parse(selected.registration.schema));
        }
    };

    const handleSave = async () => {
        register.eventStore = params.eventStore!;
        register.types = [{
            type: currentRegistration.type,
            owner: currentRegistration.owner,
            source: EventTypeSource.user,
            schema: JSON.stringify(schema, null, 2),
            generations: [],
            migrations: [],
            eventStore: params.eventStore!
        }];

        await register.execute();
    };

    const [typeFormatsQuery] = AllTypeFormats.use();

    const handleSchemaChange = (newSchema: JsonSchema) => {
        setSchema(newSchema);
    };

    const canEdit = currentRegistration.source !== EventTypeSource.code;
    const canEditReason = !canEdit ? strings.eventStore.general.eventTypes.cannotEditReason : undefined;

    return (
        <div className="type-details" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            {hasMultipleGenerations && (
                <div style={{ padding: '8px 16px', borderBottom: '1px solid var(--surface-border)', display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <label style={{ color: 'var(--text-color-secondary)', fontSize: '0.875rem' }}>
                        {strings.eventStore.general.eventTypes.columns.generation}:
                    </label>
                    <Dropdown
                        value={selectedGeneration}
                        options={generationOptions}
                        optionLabel='label'
                        optionValue='value'
                        onChange={(value) => handleGenerationChange(value)}
                        style={{ minWidth: '160px' }}
                    />
                </div>
            )}
            <Tabs className='flex flex-col flex-1 min-h-0'
                panelContainerClassName='flex flex-col flex-1 min-h-0 p-0'>
                <TabPanel header={strings.eventStore.general.eventTypes.tabs.schema}>
                    <div className='flex flex-col flex-1 min-h-0'>
                        <SchemaEditor
                            schema={schema}
                            eventTypeName={currentRegistration.type.id}
                            canEdit={canEdit}
                            canNotEditReason={canEditReason}
                            onChange={handleSchemaChange}
                            onSave={handleSave}
                            typeFormats={typeFormatsQuery.data}
                        />
                    </div>
                </TabPanel>
                <TabPanel header={strings.eventStore.general.eventTypes.tabs.observers}>
                    <div className='flex flex-col flex-1 min-h-0' style={{ padding: '0 16px 16px 16px' }}>
                        <ObserversForEventType eventTypeId={currentRegistration.type.id} />
                    </div>
                </TabPanel>
            </Tabs>
        </div>
    );
};
