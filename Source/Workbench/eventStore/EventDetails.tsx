// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TreeItem, TreeItemProps, TreeView, treeItemClasses } from '@mui/lab';
import { Box, SvgIconProps, Typography, styled } from '@mui/material';
import { AppendedEventWithJsonAsContent as AppendedEvent } from 'API/events/store/sequence/AppendedEventWithJsonAsContent';
import { EventTypeInformation } from 'API/events/store/types/EventTypeInformation';
import * as icons from '@mui/icons-material';
import { PropertyType, Schema } from './Schema';

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
    schemas: any[];
}

function getIconFor(propertyPath: string, schema: Schema) {
    const propertyType = schema.getPropertyType(propertyPath);
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
        case PropertyType.Enum:
            return icons.ListAlt;
        case PropertyType.Unknown:
            return icons.QuestionMark;
    }

    return icons.QuestionMark;
}


const PropertiesFor = (props: { propertyPath: string, level: number, value: any, schema: Schema }) => {
    return (
        <>
            {
                Object.keys(props.value).map((item, index) => {
                    const fullPropertyPath = props.propertyPath === '' ? item : `${props.propertyPath}.${item}`;
                    const propertyType = props.schema.getPropertyType(fullPropertyPath);
                    if (propertyType === PropertyType.Object) {
                        return (
                            <StyledTreeItem
                                key={index}
                                nodeId={index.toString()}
                                labelText={item}
                                labelIcon={icons.DataObject}>
                                <PropertiesFor
                                    propertyPath={fullPropertyPath}
                                    level={props.level + 1}
                                    value={props.value[item]}
                                    schema={props.schema} />
                            </StyledTreeItem>
                        );
                    } else {
                        let value = props.value[item];

                        if (propertyType == PropertyType.Enum) {
                            value = props.schema.getEnumValuesAndNames(fullPropertyPath).find(_ => _.value == value)?.name || value;
                        } else {
                            value = value.toString();
                        }

                        const key = `${props.level}-${index}`;
                        return (
                            <StyledTreeItem
                                key={key}
                                nodeId={key}
                                labelText={item}
                                labelIcon={getIconFor(fullPropertyPath, props.schema)}
                                labelInfo={value} />
                        );
                    }
                })
            }
        </>
    );
};

export const EventDetails = (props: EventDetailsProps) => {
    const schemaForGeneration = props.schemas.find(_ => _.generation == props.event.metadata.type.generation);
    if (!schemaForGeneration) return (<></>);

    const schema = new Schema(schemaForGeneration);

    return (
        <TreeView
            defaultExpanded={['3']}
            defaultCollapseIcon={<icons.ArrowDropDown />}
            defaultExpandIcon={<icons.ArrowRight />}
            defaultEndIcon={<div style={{ width: 24 }} />}
            sx={{ height: 264, flexGrow: 1, maxWidth: 700, overflowY: 'auto' }}>
            <PropertiesFor propertyPath="" level={0} value={props.event.content} schema={schema} />
        </TreeView>
    );
};
