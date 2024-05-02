//  Copyright (c) Cratis. All rights reserved.
//  Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ref } from 'vue';

import { injectable, singleton } from 'tsyringe';

@injectable()
class DataService {
    constructor() { }

    async doSomething(): Promise<number> {
        return 42;
    }
}

@singleton()
class UserService {
    constructor(private readonly dataService: DataService) { }

    async login(): Promise<number> {
        return await this.dataService.doSomething();
    }
}

export default {
    services: {
        userService: UserService
    },
    setup(props, context) {
        console.log(props);

        //console.log(context.expose);
        const count = ref(0);
        context.expose({ count });
    },
    data() {
        return {
            message: "Hello from Workbench!",
            count: 0
        };
    },
    computed: {
        countMessage() {
            return `${this.message} ${this.count}`;
        }
    },
    methods: {
        async sayHello() {
            this.message = "Yalmmar.";

            const result = await this.$services.userService.login();
            this.count += result;
        }
    }
};
