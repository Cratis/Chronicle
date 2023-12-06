

export interface Item {
    children?: Item[];
    isEditable: boolean;
    isAddButton?: boolean;
    name: string | undefined;
}

export interface EditState {
    name?: string;
    path: number[],
    isEditing: boolean,
    isAdding: boolean,
}
