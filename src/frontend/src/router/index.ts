import { createRouter, createWebHistory } from "vue-router";
import LoginForm from "../components/LoginForm.vue";
import LawPicker from "../components/LawPicker.vue";
import { lawPickerStore } from "../store/store";

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: "/",
            name: "home",
            component: LoginForm,
        },
        {
            path: "/picker",
            name: "law picker",
            component: LawPicker,
            meta: {
                requiresAuth: true
            }
        }
    ]
});

router.beforeEach((to, _from, next) => {
    const store = lawPickerStore();
    const authenticated = localStorage.getItem('authenticated');


    if (to.path === "/" && authenticated) {
        next("/picker")
    } else if (to.meta.requiresAuth && !store.isAuthenticated && !authenticated) {
        next("/")
    } else {
        next()
    }
})

export default router;