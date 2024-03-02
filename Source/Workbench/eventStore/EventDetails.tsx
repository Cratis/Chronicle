// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TreeItem, TreeItemProps, TreeView, treeItemClasses } from '@mui/lab';
import { Box, SvgIconProps, Typography, alpha, styled } from '@mui/material';
import { AppendedEventWithJsonAsContent as AppendedEvent } from 'API/events/store/sequence/AppendedEventWithJsonAsContent';
import { EventTypeInformation } from 'API/events/store/types/EventTypeInformation';
import * as icons from '@mui/icons-material';
import { EnumValueAndName, PropertyType, Schema } from './Schema';
import { Constructor, Fields, Field } from '@aksio/fundamentals';
import { EventContext } from 'API/events/store/sequence/EventContext';
import { Causation } from '../API/events/store/sequence/Causation';

type StyledTreeItemProps = TreeItemProps & {
    labelIcon: React.ElementType<SvgIconProps>;
    labelInfo?: string;
    labelText: string;
};

const StyledTreeItemRoot = styled(TreeItem)(({ theme }) => ({
    color: theme.palette.text.secondary,
    [`& .${treeItemClasses.content}`]: {
        color: theme.palette.text.secondary,
        borderTopRightRadius: theme.spacing(2),
        borderBottomRightRadius: theme.spacing(2),
        paddingRight: theme.spacing(1),
        fontWeight: theme.typography.fontWeightMedium,
        '&.Mui-expanded': {
            fontWeight: theme.typography.fontWeightRegular,
        },
        '&:hover': {
            backgroundColor: theme.palette.action.hover,
        },
        '&.Mui-focused, &.Mui-selected, &.Mui-selected.Mui-focused': {
            backgroundColor: `var(--tree-view-bg-color, ${theme.palette.action.selected})`,
            color: 'var(--tree-view-color)',
        },
        [`& .${treeItemClasses.label}`]: {
            fontWeight: 'inherit',
            color: 'inherit',
        },
    },
    [`& .${treeItemClasses.group}`]: {
        marginLeft: 0,
        [`& .${treeItemClasses.content}`]: {
            paddingLeft: theme.spacing(2),
        },
    },
}));

function StyledTreeItem(props: StyledTreeItemProps) {
    const {
        labelIcon: LabelIcon,
        labelInfo,
        labelText,
        ...other
    } = props;

    return (
        <StyledTreeItemRoot
            label={
                <Box sx={{ display: 'flex', alignItems: 'center', p: 0.5, pr: 0 }}>
                    <Box component={LabelIcon} color="inherit" sx={{ mr: 1 }} />
                    <Typography variant="body2" sx={{ fontWeight: 'inherit', flexGrow: 1 }}>
                        {labelText}
                    </Typography>
                    <Typography variant="caption" color="inherit">
                        {labelInfo}
                    </Typography>
                </Box>
            }
            {...other}
        />
    );
}

export interface EventDetailsProps {
    event: AppendedEvent;
    type: EventTypeInformation;
    schema: any;
}

function getIconFor(propertyType: PropertyType) {
    switch (propertyType) {
        case PropertyType.String:
            return icons.TextFormat;
        case PropertyType.Byte:
            return icons.Biotech;
        case PropertyType.Number:
            return icons.Numbers;
        case PropertyType.Boolean:
            return icons.ToggleOn;
        case PropertyType.Date:
            return icons.DateRange;
        case PropertyType.Guid:
            return icons.Info;
        case PropertyType.Object:
            return icons.DataObject;
        case PropertyType.Array:
            return icons.List;
        case PropertyType.Enum:
            return icons.ListAlt;
        case PropertyType.Unknown:
            return icons.QuestionMark;
    }

    return icons.QuestionMark;
}

type SchemaOrType = Schema | Constructor;

const PropertiesFor = (props: { propertyPath: string, value: any, schemaOrType: SchemaOrType }) => {
    const fields: Field[] = [];
    if (!(props.schemaOrType instanceof Schema)) {
        fields.push(...Fields.getFieldsForType(props.schemaOrType));
    }

    const getPropertyType = (fullPropertyPath: string, item: any) => {
        if (props.schemaOrType instanceof Schema) {
            return props.schemaOrType.getPropertyType(fullPropertyPath);
        }

        const segments = fullPropertyPath.split('.');
        let field: Field | undefined = undefined;
        let currentFields = fields;
        for (const segment of segments) {
            field = currentFields.find(_ => _.name == segment);
            if (field) {
                currentFields = Fields.getFieldsForType(field.type);
            }
        }

        if (field?.enumerable) return PropertyType.Array;

        let constructor = field?.type;
        if (!constructor) {
            constructor = item[fullPropertyPath]?.constructor;
        }
        if (constructor) {
            switch (constructor) {
                case String:
                    return PropertyType.String;

                case Number:
                    return PropertyType.Number;

                case Boolean:
                    return PropertyType.Boolean;

                case Date:
                    return PropertyType.Date;

                case Object:
                    return PropertyType.Object;
            }

            return PropertyType.Object;
        }

        return PropertyType.Unknown;
    }

    const getEnumValuesAndNames = (fullPropertyPath: string): EnumValueAndName[] => {
        if (props.schemaOrType instanceof Schema) {
            props.schemaOrType.getEnumValuesAndNames(fullPropertyPath)
        }

        return []
    }

    const getTextForArrayItem = (item: any, index: number) => {
        if (item instanceof Causation) {
            return item.type;
        }

        return index.toString();
    }

    const getLabelForArrayItem = (item: any, index: number) => {
        if (item instanceof Causation) {
            return item.occurred.toLocaleString();
        }

        return '';
    }

    const getPropertiesForArrayItem = (item: any, index: number) => {
        if (item instanceof Causation) {
            return item.properties;
        }

        return Object.keys(item).sort();
    }

    return (
        <>
            {
                Object.keys(props.value).sort().map((item, index) => {
                    let value = props.value[item];
                    if (value === null || value === undefined) return (<></>);

                    const fullPropertyPath = props.propertyPath === '' ? item : `${props.propertyPath}.${item}`;
                    const key = `${fullPropertyPath}`;
                    const propertyType = getPropertyType(fullPropertyPath, props.value);

                    if (propertyType === PropertyType.Object) {
                        return (
                            <StyledTreeItem
                                key={key}
                                nodeId={key}
                                labelText={item}
                                labelIcon={icons.DataObject}>
                                <PropertiesFor
                                    propertyPath={fullPropertyPath}
                                    value={value}
                                    schemaOrType={props.schemaOrType} />
                            </StyledTreeItem>
                        );
                    } if (propertyType === PropertyType.Array) {
                        return (
                            <StyledTreeItem
                                key={key}
                                nodeId={key}
                                labelText={item}
                                labelIcon={icons.DataArray}>

                                {props.value[item].map((arrayItem: any, arrayIndex) => {
                                    return (
                                        <StyledTreeItem
                                            key={`${key}-${arrayIndex}`}
                                            nodeId={`${key}-${arrayIndex}`}
                                            labelText={getTextForArrayItem(arrayItem, arrayIndex)}
                                            labelInfo={getLabelForArrayItem(arrayItem, arrayIndex)}
                                            labelIcon={icons.DataObject}>

                                            <PropertiesFor
                                                propertyPath=""
                                                value={getPropertiesForArrayItem(arrayItem, arrayIndex)}
                                                schemaOrType={Causation} />

                                        </StyledTreeItem>
                                    )
                                })}
                            </StyledTreeItem>
                        );
                    } else {
                        if (propertyType == PropertyType.Enum) {
                            value = getEnumValuesAndNames(fullPropertyPath).find(_ => _.value == value)?.name || value;
                        } else if (propertyType == PropertyType.Date) {
                            value = new Date(value.toString()).toLocaleString();
                        } else {
                            value = value.toString();
                        }

                        return (
                            <StyledTreeItem
                                key={key}
                                nodeId={key}
                                labelText={item}
                                labelIcon={getIconFor(propertyType)}
                                labelInfo={value} />
                        );
                    }
                })
            }
        </>
    );
};

export const EventDetails = (props: EventDetailsProps) => {
    if (!props.schema) return (<></>);

    const schema = new Schema(props.schema);

    const fields = Fields.getFieldsForType(EventContext);

    return (
        <TreeView
            defaultCollapseIcon={<icons.ArrowDropDown />}
            defaultExpandIcon={<icons.ArrowRight />}
            defaultEndIcon={<div style={{ width: 24 }} />}
            sx={{ height: '100%', flexGrow: 1, maxWidth: 700, overflowY: 'auto' }}>
            <StyledTreeItem nodeId="EventContext" labelText="Context" labelIcon={icons.BorderAllOutlined}>
                <PropertiesFor propertyPath="" value={props.event.context} schemaOrType={EventContext} />
            </StyledTreeItem>
            <PropertiesFor propertyPath="" value={props.event.content} schemaOrType={schema} />
        </TreeView>
    );
};
