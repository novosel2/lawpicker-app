<script setup lang="ts">
import { ref, watch } from 'vue';
import { lawPickerStore } from '../store/store';
import { useRouter } from 'vue-router';

const router = useRouter();
const languages = [
    {
        Code: "BG",
        Name: "Bulgarian"
    },
    {
        Code: "CS",
        Name: "Czech"
    },
    {
        Code: "DA",
        Name: "Danish"
    },
    {
        Code: "DE",
        Name: "German"
    },
    {
        Code: "EL",
        Name: "Greek"
    },
    {
        Code: "EN",
        Name: "English"
    },
    {
        Code: "ES",
        Name: "Spanish"
    },
    {
        Code: "ET",
        Name: "Estonian"
    },
    {
        Code: "FI",
        Name: "Finnish"
    },
    {
        Code: "FR",
        Name: "French"
    },
    {
        Code: "GA",
        Name: "Irish"
    },
    {
        Code: "HR",
        Name: "Croatian"
    },
    {
        Code: "HU",
        Name: "Hungarian"
    },
    {
        Code: "IT",
        Name: "Italian"
    },
    {
        Code: "LT",
        Name: "Lithuanian"
    },
    {
        Code: "LV",
        Name: "Latvian"
    },
    {
        Code: "MT",
        Name: "Maltese"
    },
    {
        Code: "NL",
        Name: "Dutch"
    },
    {
        Code: "PL",
        Name: "Polish"
    },
    {
        Code: "PT",
        Name: "Portuguese"
    },
    {
        Code: "RO",
        Name: "Romanian"
    },
    {
        Code: "SK",
        Name: "Slovak"
    },
    {
        Code: "SL",
        Name: "Slovene"
    },
    {
        Code: "SV",
        Name: "Swedish"
    }
];
const lawTypes = [
    {
        Code: "D",
        Name: "Decision"
    },
    {
        Code: "R",
        Name: "Regulation"
    },
    {
        Code: "L",
        Name: "Directive"
    }
]

const selectedLang = ref("EN"); // Store just the code
const selectedLawType = ref("L");
const search = ref("");
const page = ref(1);

const logout = () => {
    localStorage.clear();
    const store = lawPickerStore();
    store.authenticate();
    router.push('/');
}
</script>

<template>
    <main>
        <h1>Law picker</h1>
        <div class="input-div">
            <input class="search-input" type="text" v-model="search" placeholder="european labour...">
            <select name="lawType" id="lawType" v-model="selectedLawType">
                <option v-for="lawType in lawTypes" :key="lawType.Code" :value="lawType.Code">
                    {{ lawType.Name }}
                </option>
            </select>
            <select name="lang" id="lang" v-model="selectedLang">
                <option v-for="language in languages" :key="language.Code" :value="language.Code">
                    {{ language.Name }}
                </option>
            </select>
            <input type="submit" />
        </div>
    </main>
</template>

<style scoped>
    main {
        display: flex;
        flex-direction: column;
        align-items: center;
        width: 40vw;
        margin: 0 auto;
        margin-top: 10vh;
        padding: 20px;
        gap: 25px;
    }

    .input-div {
        display: flex;
        gap: 10px;
        width: 100%;
    }

    .input-div * {
        height: 30px;
    }

    .search-input {
        flex: 1;
        padding-left: 10px;:display;
    }
</style>
