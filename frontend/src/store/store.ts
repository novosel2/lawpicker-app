import { defineStore } from "pinia";

export const lawPickerStore = defineStore("lawStore", {
    state: () => ({ isAuthenticated: false }),
    actions: {
        authenticate() {
            this.isAuthenticated = !this.isAuthenticated;
        }
    }
})