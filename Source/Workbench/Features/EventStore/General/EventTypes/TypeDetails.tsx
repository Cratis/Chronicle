// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';
import { IDetailsComponentProps } from '@cratis/components/DataPage';
import { SchemaEditor as _SE } from '@cratis/components';
const SchemaEditor = _SE.SchemaEditor;
import type { JsonSchema } from '@cratis/components/types';
import { AllTypeFormats } from 'Api/TypeFormats';
import { EventTypeRegistration, EventTypeSource } from 'Api/Events';
import { Register } from 'Api/Events';
import { AllEventTypeGenerations } from 'Api/EventTypes/AllEventTypeGenerations';
import { Dropdown } from 'primereact/dropdown';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';

interface GenerationOption {
    label: string;
    value: number;
    registration: EventTypeRegistration;
}

export const TypeDetails = (props: IDetailsComponentProps<EventTypeRegistration>) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [schema, setSchema] = useState<JsonSchema>(() => JSON.parse(props.item.schema));
    const [selectedGeneration, setSelectedGeneration] = useState<number>(props.item.type.generation);
    const [currentRegistration, setCurrentRegistration] = useState<EventTypeRegistration>(props.item);
    const [register] = Register.use();
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
        const selected = generationOptions.find(opt => opt.value === generation);
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
            schema: JSON.stringify(schema, null, 2)
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
                        onChange={(e) => handleGenerationChange(e.value)}
                        style={{ minWidth: '160px' }}
                    />
                </div>
            )}
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
    );
};
