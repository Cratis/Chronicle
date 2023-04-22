// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TreeItem, TreeItemProps, TreeView, treeItemClasses } from '@mui/lab';
import { Box, SvgIconProps, Typography, styled } from '@mui/material';
import { AppendedEventWithJsonAsContent as AppendedEvent } from 'API/events/store/sequence/AppendedEventWithJsonAsContent';
import { EventTypeInformation } from 'API/events/store/types/EventTypeInformation';
import * as icons from '@mui/icons-material';

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

function getIconFor(value: any) {
    if (value instanceof Date) {
        return icons.DateRange;
    } else if (typeof value == "number") {
        return icons.Numbers;
    } else if (typeof value == "string") {
        return icons.TextFormat;
    } else if (typeof value == "boolean") {
        return icons.ToggleOn;
    } else if (typeof value == "object") {
        return icons.DataObject;
    }
    return icons.QuestionMark;
}


const PropertiesFor = (props: { level: number, value: any }) => {
    return (
        <>
            {
                Object.keys(props.value).map((item, index) => {
                    if (typeof props.value[item] === 'object') {
                        return (
                            <StyledTreeItem
                                key={index}
                                nodeId={index.toString()}
                                labelText={item}
                                labelIcon={icons.DataObject}>
                                <PropertiesFor level={props.level + 1} value={props.value[item]} />
                            </StyledTreeItem>
                        );
                    } else {
                        const key = `${props.level}-${index}`;
                        return (
                            <StyledTreeItem
                                key={key}
                                nodeId={key}
                                labelText={item}
                                labelIcon={getIconFor(props.value[item])}
                                labelInfo={props.value[item].toString()} />
                        );
                    }
                })
            }
        </>
    );
};

export const EventDetails = (props: EventDetailsProps) => {

    return (
        <TreeView
            defaultExpanded={['3']}
            defaultCollapseIcon={<icons.ArrowDropDown />}
            defaultExpandIcon={<icons.ArrowRight />}
            defaultEndIcon={<div style={{ width: 24 }} />}
            sx={{ height: 264, flexGrow: 1, maxWidth: 700, overflowY: 'auto' }}>
            <PropertiesFor level={0} value={props.event.content} />
        </TreeView>
    );
};
