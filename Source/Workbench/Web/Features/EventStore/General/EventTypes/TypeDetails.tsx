// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Column } from 'primereact/column';
import { TreeNode } from 'primereact/treenode';
import { TreeTable } from 'primereact/treetable';
import { Menubar } from 'primereact/menubar';
import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { Dropdown } from 'primereact/dropdown';
import { IDetailsComponentProps } from 'Components';
import { EventTypeRegistration } from 'Api/Events';
import { Register } from 'Api/Events';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import * as faIcons from 'react-icons/fa6';

const formatType = (type: string) => {
    switch (type) {
        case 'date-time-offset': return 'DateTimeOffset';
        case 'date-time': return 'DateTime';
        case 'date-only': return 'DateOnly';
        case 'time-only': return 'TimeOnly';
        case 'duration': return 'TimeSpan';
    }

    return type;
};

const reverseFormatType = (type: string): { type?: string; format?: string } => {
    switch (type) {
        case 'DateTimeOffset': return { type: 'string', format: 'date-time-offset' };
        case 'DateTime': return { type: 'string', format: 'date-time' };
        case 'DateOnly': return { type: 'string', format: 'date-only' };
        case 'TimeOnly': return { type: 'string', format: 'time-only' };
        case 'TimeSpan': return { type: 'string', format: 'duration' };
        case 'string': return { type: 'string' };
        case 'number': return { type: 'number' };
        case 'integer': return { type: 'integer' };
        case 'boolean': return { type: 'boolean' };
        case 'array': return { type: 'array' };
        case 'object': return { type: 'object' };
        default: return { type };
    }
};

interface Property {
    name: string;
    type: string;
}

const availableTypes = [
    { label: 'String', value: 'string' },
    { label: 'Number', value: 'number' },
    { label: 'Integer', value: 'integer' },
    { label: 'Boolean', value: 'boolean' },
    { label: 'Array', value: 'array' },
    { label: 'Object', value: 'object' },
    { label: 'DateTimeOffset', value: 'DateTimeOffset' },
    { label: 'DateTime', value: 'DateTime' },
    { label: 'DateOnly', value: 'DateOnly' },
    { label: 'TimeOnly', value: 'TimeOnly' },
    { label: 'TimeSpan', value: 'TimeSpan' }
];

export const TypeDetails = (props: IDetailsComponentProps<EventTypeRegistration>) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [isEditMode, setIsEditMode] = useState(false);
    const [properties, setProperties] = useState<Property[]>(() => {
        const schema = JSON.parse(props.item.schema);
        const schemaProperties = schema.properties || {};
        const propertyList: Property[] = [];

        for (const propertyName in schemaProperties) {
            const property = schemaProperties[propertyName];
            let type = property.format;
            if (!property.format) {
                type = property.type;
                if (Array.isArray(type)) {
                    type = type[type.length - 1];
                }
            }

            propertyList.push({
                name: propertyName,
                type: formatType(type)
            });
        }

        return propertyList;
    });

    const [register] = Register.use();

    const handleEdit = () => {
        setIsEditMode(true);
    };

    const handleCancel = () => {
        setIsEditMode(false);
        // Reset properties to original state
        const schema = JSON.parse(props.item.schema);
        const schemaProperties = schema.properties || {};
        const propertyList: Property[] = [];

        for (const propertyName in schemaProperties) {
            const property = schemaProperties[propertyName];
            let type = property.format;
            if (!property.format) {
                type = property.type;
                if (Array.isArray(type)) {
                    type = type[type.length - 1];
                }
            }

            propertyList.push({
                name: propertyName,
                type: formatType(type)
            });
        }

        setProperties(propertyList);
    };

    const handleSave = async () => {
        const schema: any = {
            $schema: 'http://json-schema.org/draft-07/schema#',
            type: 'object',
            properties: {}
        };

        properties.forEach(prop => {
            const typeInfo = reverseFormatType(prop.type);
            if (typeInfo.format) {
                schema.properties[prop.name] = {
                    type: typeInfo.type,
                    format: typeInfo.format
                };
            } else {
                schema.properties[prop.name] = {
                    type: typeInfo.type
                };
            }
        });

        register.eventStore = params.eventStore!;
        register.types = [{
            type: props.item.type,
            schema: JSON.stringify(schema, null, 2)
        }];

        await register.execute();
        setIsEditMode(false);
    };

    const handleAddProperty = () => {
        setProperties([...properties, { name: 'newProperty', type: 'string' }]);
    };

    const handlePropertyNameChange = (index: number, newName: string) => {
        const updated = [...properties];
        updated[index].name = newName;
        setProperties(updated);
    };

    const handlePropertyTypeChange = (index: number, newType: string) => {
        const updated = [...properties];
        updated[index].type = newType;
        setProperties(updated);
    };

    const handleRemoveProperty = (index: number) => {
        const updated = properties.filter((_, i) => i !== index);
        setProperties(updated);
    };

    const propertyNodes: TreeNode[] = properties.map((prop, index) => ({
        key: index,
        data: {
            name: prop.name,
            value: prop.type,
            index
        }
    }));

    const nameBodyTemplate = (node: TreeNode) => {
        const index = node.data.index;
        if (isEditMode) {
            return (
                <InputText
                    value={node.data.name}
                    onChange={(e) => handlePropertyNameChange(index, e.target.value)}
                />
            );
        }
        return node.data.name;
    };

    const valueBodyTemplate = (node: TreeNode) => {
        const index = node.data.index;
        if (isEditMode) {
            return (
                <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                    <Dropdown
                        value={node.data.value}
                        options={availableTypes}
                        onChange={(e) => handlePropertyTypeChange(index, e.value)}
                    />
                    <Button
                        icon="pi pi-trash"
                        severity="danger"
                        text
                        onClick={() => handleRemoveProperty(index)}
                    />
                </div>
            );
        }
        return node.data.value;
    };

    const menuItems = [
        ...(!isEditMode ? [{
            label: 'Edit',
            icon: <faIcons.FaPencil className='mr-2' />,
            command: handleEdit
        }] : []),
        ...(isEditMode ? [{
            label: 'Add Property',
            icon: <faIcons.FaPlus className='mr-2' />,
            command: handleAddProperty
        }, {
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
            <div style={{ flex: 1, overflow: 'auto', padding: '1rem' }}>
                <TreeTable value={propertyNodes} showGridlines={false}>
                    <Column field='name' header='Property' expander body={nameBodyTemplate} />
                    <Column field='value' header='Type' body={valueBodyTemplate} />
                </TreeTable>
            </div>
        </div>
    );
};
