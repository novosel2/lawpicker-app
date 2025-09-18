<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { lawPickerStore } from '../store/store';

const router = useRouter();
const store = lawPickerStore()

const rilUsername = "markic";
const rilPass = "pass";

const username = ref("");
const password = ref("");

const error = ref("");

const signIn = () => {
  if (username.value === rilUsername && password.value === rilPass) {
    store.authenticate();
    localStorage.setItem('authenticated', 'true');
    router.push("/picker");
  } else {
    error.value = "Wrong username or password, please try again!"
  }
};
</script>

<template>
  <form @submit.prevent="signIn">
    <div>
      <label for="username">Username: </label>
      <input type="text" placeholder="markič" name="username" v-model="username">
    </div>
    <div>
      <label for="password">Password: </label>
      <input type="password" name="password" v-model="password" placeholder="*******">
    </div>
    <p v-if="error">{{ error }}</p>
    <input type="submit" value="Sign in" class="submitBtn">
  </form>
</template>

<style scoped>
  form {
    display: flex;
    flex-direction: column;
    place-items: center;
    justify-content: center;
    height: 100dvh;
    gap: 12px;
  }

  div {
    display: flex;
    flex-direction: column;
    width: 200px;
  }

  p {
    width: 200px;
    color: red;
  }

  .submitBtn {
    font-size: 16px;
    padding: 5px 0;
    background-color: #242424;
    border: 1px solid gray;
    cursor: pointer;
    transition: 0.2s ease;
    width: 200px;
  }

  .submitBtn:hover {
    background-color: gray;
    border-color: #242424;
    color: #000;
  }
</style>
