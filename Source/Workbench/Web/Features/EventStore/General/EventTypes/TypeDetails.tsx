// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Menubar } from 'primereact/menubar';
import { IDetailsComponentProps } from 'Components';
import { JSONSchemaEditor } from 'Components';
import { EventTypeRegistration } from 'Api/Events';
import { Register } from 'Api/Events';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import * as faIcons from 'react-icons/fa6';

interface JSONSchemaType {
    type?: string;
    format?: string;
    properties?: Record<string, JSONSchemaType>;
    items?: JSONSchemaType;
    required?: string[];
}

export const TypeDetails = (props: IDetailsComponentProps<EventTypeRegistration>) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [isEditMode, setIsEditMode] = useState(false);
    const [schema, setSchema] = useState<JSONSchemaType>(() => JSON.parse(props.item.schema));
    const [register] = Register.use();

    const handleEdit = () => {
        setIsEditMode(true);
    };

    const handleCancel = () => {
        setIsEditMode(false);
        setSchema(JSON.parse(props.item.schema));
    };

    const handleSave = async () => {
        register.eventStore = params.eventStore!;
        register.types = [{
            type: props.item.type,
            schema: JSON.stringify(schema, null, 2)
        }];

        await register.execute();
        setIsEditMode(false);
    };

    const handleSchemaChange = (newSchema: JSONSchemaType) => {
        setSchema(newSchema);
    };

    const menuItems = [
        ...(!isEditMode ? [{
            label: 'Edit',
            icon: <faIcons.FaPencil className='mr-2' />,
            command: handleEdit
        }] : []),
        ...(isEditMode ? [{
            label: 'Save',
            icon: <faIcons.FaCheck className='mr-2' />,
            command: handleSave
        }, {
            label: 'Cancel',
            icon: <faIcons.FaXmark className='mr-2' />,
            command: handleCancel
        }] : [])
    ];

    return (
        <div className="type-details" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <div className="px-4 py-2">
                <Menubar aria-label="Actions" model={menuItems} />
            </div>
            <div style={{ flex: 1, overflow: 'hidden' }}>
                <JSONSchemaEditor
                    schema={schema}
                    eventTypeName={props.item.type}
                    isEditMode={isEditMode}
                    onChange={handleSchemaChange}
                />
            </div>
        </div>
    );
};
