export interface IBookmarkNode {
    key: string;
    label: string;
    icon?: string;
    data: string;
    children?: IBookmarkNode[];
}

export type IBookmarkNodes = IBookmarkNode;


export const BookmarkNodes = {
    getBookmarkNodesData() {
        return [
            {
                key: '0',
                label: 'My queries',
                data: 'My queries data here',
                children: [
                    {
                        key: '0-0',
                        label: 'People',
                        data: 'People data goes here',
                        icon: 'pi pi-fw pi-inbox',
                        children: [
                            {
                                key: '0-0-0',
                                label: 'All people',
                                icon: 'pi pi-fw pi-file',
                                data: 'All people data goes here',
                            },
                            {
                                key: '0-0-1',
                                label: 'Specific people',
                                icon: 'pi pi-fw pi-file',
                                data: 'Specific people data goes here',
                            },
                        ],
                    },
                    {
                        key: '0-1',
                        label: 'Work',
                        data: 'Work data goes here',
                        icon: 'pi pi-fw pi-inbox',
                        children: [
                            {
                                key: '0-1-0',
                                label: 'Work 1',
                                icon: 'pi pi-fw pi-file',
                                data: 'Data here',
                            },
                        ],
                    },
                    {
                        key: '0-2',
                        label: 'Payment',
                        data: 'Payment data goes here',
                        icon: 'pi pi-fw pi-inbox',
                        children: [
                            {
                                key: '0-2-0',
                                label: 'All payment',
                                icon: 'pi pi-fw pi-file',
                                data: 'Data here',
                            },
                            {
                                key: '0-2-1',
                                label: 'June payment',
                                icon: 'pi pi-fw pi-file',
                                data: 'Data here',
                            },
                        ],
                    },
                ],
            },
            {
                key: '1',
                label: 'Shared queries',
                data: 'Shared queries data here',
                children: [
                    {
                        key: '0-0',
                        label: 'People',
                        data: 'People data goes here',
                        icon: 'pi pi-fw pi-inbox',
                        children: [
                            {
                                key: '0-0-0',
                                label: 'All people',
                                icon: 'pi pi-fw pi-file',
                                data: 'All people data goes here',
                            },
                            {
                                key: '0-0-1',
                                label: 'Specific people',
                                icon: 'pi pi-fw pi-file',
                                data: 'Specific people data goes here',
                            },
                        ],
                    },
                    {
                        key: '0-1',
                        label: 'Work',
                        data: 'Work data goes here',
                        icon: 'pi pi-fw pi-inbox',
                        children: [
                            {
                                key: '0-1-0',
                                label: 'Work 1',
                                icon: 'pi pi-fw pi-file',
                                data: 'Data here',
                            },
                        ],
                    },
                    {
                        key: '0-2',
                        label: 'Payment',
                        data: 'Payment data goes here',
                        icon: 'pi pi-fw pi-inbox',
                        children: [
                            {
                                key: '0-2-0',
                                label: 'All payment',
                                icon: 'pi pi-fw pi-file',
                                data: 'Data here',
                            },
                            {
                                key: '0-2-1',
                                label: 'June payment',
                                icon: 'pi pi-fw pi-file',
                                data: 'Data here',
                            },
                        ],
                    },
                ],
            },
        ];
    },

    getBookmarkNodes() {
        return Promise.resolve(this.getBookmarkNodesData());
    },
};
