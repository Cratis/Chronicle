import { Item } from './types';

export const initialData: Item[] = [
    {
        name: 'My Queries',
        isEditable: false,
    },
    {
        name: 'Shared Queries',
        isEditable: false,
        isAddButton: true,
        children: [
            {
                name: 'People', isEditable: true,
                children:
                    [
                        { name: 'All people', isEditable: false, },
                        { name: 'Specific people', isEditable: false, }
                    ]
            },
            { name: 'Work', isEditable: true, },
            {
                name: 'Payment', isEditable: true,
                children: [
                    { name: 'All people', isEditable: false, },
                    { name: 'Specific people', isEditable: false, }
                ]
            },
        ],
    },
];




export const arraysEqual = (a: any[], b: any[]): boolean => {
    return a.length === b.length && a.every((val, index) => val === b[index]);
};

export const getItemByPath = (data: any[], path: number[]): any => {
    return path.reduce((current, index) => current.children[index], {
        children: data,
    });
};



