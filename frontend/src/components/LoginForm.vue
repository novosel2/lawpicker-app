<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { lawPickerStore } from '../store/store';
import axios from 'axios';

const router = useRouter();
const store = lawPickerStore();

// Form state
const isLoginMode = ref(true);
const loading = ref(false);
const error = ref("");

// Login form data
const loginData = ref({
  loginName: "",
  password: ""
});

// Registration form data  
const registerData = ref({
  username: "",
  email: "",
  password: "",
  confirmPassword: ""
});

// Toggle between login and register
const toggleMode = () => {
  isLoginMode.value = !isLoginMode.value;
  error.value = "";
  // Clear forms when switching
  loginData.value = { loginName: "", password: "" };
  registerData.value = { username: "", email: "", password: "", confirmPassword: "" };
};

// Validation
const validateLogin = () => {
  if (!loginData.value.loginName.trim()) {
    error.value = "Username is required";
    return false;
  }
  if (!loginData.value.password) {
    error.value = "Password is required"; 
    return false;
  }
  return true;
};

const validateRegister = () => {
  const { username, email, password, confirmPassword } = registerData.value;
  
  if (!username.trim()) {
    error.value = "Username is required";
    return false;
  }
  if (!email.trim()) {
    error.value = "Email is required";
    return false;
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    error.value = "Please enter a valid email address";
    return false;
  }
  if (!password) {
    error.value = "Password is required";
    return false;
  }
  if (password.length < 6) {
    error.value = "Password must be at least 6 characters long";
    return false;
  }
  if (password !== confirmPassword) {
    error.value = "Passwords do not match";
    return false;
  }
  return true;
};

// API calls
const signIn = async () => {
  if (!validateLogin()) return;
  
  loading.value = true;
  error.value = "";
  
  try {
    const response = await axios.post('http://localhost:8000/api/auth/login', {
      loginName: loginData.value.loginName,
      password: loginData.value.password
    }, {
      timeout: 10000,
      withCredentials: true
    });
    
    if (response.data && response.data.token) {
      // Store JWT token
      localStorage.setItem('jwt_token', response.data.token);
      localStorage.setItem('user_data', JSON.stringify({
        username: response.data.username,
        email: response.data.email
      }));
      localStorage.setItem('authenticated', 'true');
      
      // Set default axios header for future requests
      axios.defaults.headers.common['Authorization'] = `Bearer ${response.data.token}`;
      
      store.authenticate();
      router.push("/picker");
    } else {
      error.value = "Invalid response from server";
    }
  } catch (err: any) {
    console.error('Login error:', err);
    if (err.response?.status === 401) {
      error.value = "Invalid username or password";
    } else if (err.code === 'ECONNABORTED') {
      error.value = "Request timeout. Please try again.";
    } else {
      error.value = err.response?.data?.message || "Login failed. Please try again.";
    }
  } finally {
    loading.value = false;
  }
};

const register = async () => {
  if (!validateRegister()) return;
  
  loading.value = true;
  error.value = "";
  
  try {
    const response = await axios.post('http://localhost:8000/api/auth/register', {
      username: registerData.value.username,
      email: registerData.value.email,
      password: registerData.value.password,
      confirmPassword: registerData.value.confirmPassword
    }, {
      timeout: 10000,
      withCredentials: true
    });
    
    if (response.data && response.data.token) {
      // Store JWT token
      localStorage.setItem('jwt_token', response.data.token);
      localStorage.setItem('user_data', JSON.stringify({
        username: response.data.username,
        email: response.data.email
      }));
      localStorage.setItem('authenticated', 'true');
      
      // Set default axios header for future requests
      axios.defaults.headers.common['Authorization'] = `Bearer ${response.data.token}`;
      
      store.authenticate();
      router.push("/picker");
    } else {
      error.value = "Registration failed. Invalid response from server.";
    }
  } catch (err: any) {
    console.error('Registration error:', err);
    if (err.response?.status === 400) {
      error.value = err.response.data?.message || "Registration failed. Please check your information.";
    } else if (err.response?.status === 409) {
      error.value = "Username or email already exists";
    } else if (err.code === 'ECONNABORTED') {
      error.value = "Request timeout. Please try again.";
    } else {
      error.value = err.response?.data?.message || "Registration failed. Please try again.";
    }
  } finally {
    loading.value = false;
  }
};

// Check for existing token on component mount
const checkExistingAuth = () => {
  const token = localStorage.getItem('jwt_token');
  if (token) {
    axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
    store.authenticate();
    router.push("/picker");
  }
};

// Call on mount
checkExistingAuth();
</script>

<template>
  <div class="auth-container">
    <div class="auth-card">
      <!-- Header -->
      <div class="auth-header">
        <h1>Law Picker</h1>
        <p class="auth-subtitle">
          {{ isLoginMode ? 'Sign in to your account' : 'Create a new account' }}
        </p>
      </div>

      <!-- Loading Overlay -->
      <div v-if="loading" class="loading-overlay">
        <div class="loading-spinner"></div>
      </div>

      <!-- Login Form -->
      <form v-if="isLoginMode" @submit.prevent="signIn" class="auth-form">
        <div class="input-group">
          <label for="loginName">Username</label>
          <input 
            type="text" 
            id="loginName"
            name="loginName"
            v-model="loginData.loginName" 
            placeholder="Enter your username"
            :disabled="loading"
            required
          >
        </div>

        <div class="input-group">
          <label for="password">Password</label>
          <input 
            type="password" 
            id="password"
            name="password"
            v-model="loginData.password" 
            placeholder="Enter your password"
            :disabled="loading"
            required
          >
        </div>

        <!-- Error Message -->
        <div v-if="error" class="error-message">
          {{ error }}
        </div>

        <button type="submit" class="auth-btn" :disabled="loading">
          {{ loading ? 'Signing in...' : 'Sign In' }}
        </button>
      </form>

      <!-- Registration Form -->
      <form v-else @submit.prevent="register" class="auth-form">
        <div class="input-group">
          <label for="username">Username</label>
          <input 
            type="text" 
            id="username"
            name="username"
            v-model="registerData.username" 
            placeholder="Choose a username"
            :disabled="loading"
            required
          >
        </div>

        <div class="input-group">
          <label for="email">Email</label>
          <input 
            type="email" 
            id="email"
            name="email"
            v-model="registerData.email" 
            placeholder="Enter your email"
            :disabled="loading"
            required
          >
        </div>

        <div class="input-group">
          <label for="regPassword">Password</label>
          <input 
            type="password" 
            id="regPassword"
            name="regPassword"
            v-model="registerData.password" 
            placeholder="Create a password"
            :disabled="loading"
            required
            minlength="6"
          >
        </div>

        <div class="input-group">
          <label for="confirmPassword">Confirm Password</label>
          <input 
            type="password" 
            id="confirmPassword"
            name="confirmPassword"
            v-model="registerData.confirmPassword" 
            placeholder="Confirm your password"
            :disabled="loading"
            required
            minlength="6"
          >
        </div>

        <!-- Error Message -->
        <div v-if="error" class="error-message">
          {{ error }}
        </div>

        <button type="submit" class="auth-btn" :disabled="loading">
          {{ loading ? 'Creating Account...' : 'Create Account' }}
        </button>
      </form>

      <!-- Mode Toggle -->
      <div class="auth-toggle">
        <p>
          {{ isLoginMode ? "Don't have an account?" : "Already have an account?" }}
          <button 
            type="button" 
            @click="toggleMode" 
            class="toggle-btn"
            :disabled="loading"
          >
            {{ isLoginMode ? 'Sign up' : 'Sign in' }}
          </button>
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.auth-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f8f9fa;
  padding: 20px;
}

.auth-card {
  background: white;
  padding: 40px;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  width: 100%;
  max-width: 400px;
  position: relative;
  overflow: hidden;
}

.auth-header {
  text-align: center;
  margin-bottom: 30px;
}

.auth-header h1 {
  color: #212529;
  font-size: 2.2rem;
  font-weight: 600;
  margin-bottom: 8px;
}

.auth-subtitle {
  color: #6c757d;
  font-size: 16px;
  margin: 0;
}

/* Loading Overlay */
.loading-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(255, 255, 255, 0.9);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 10;
  border-radius: 12px;
}

.loading-spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #007bff;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.auth-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.input-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.input-group label {
  color: #495057;
  font-weight: 500;
  font-size: 14px;
}

.input-group input {
  height: 40px;
  padding: 0 12px;
  border: 1px solid #ced4da;
  border-radius: 4px;
  font-size: 14px;
  transition: all 0.2s ease;
  background: white;
}

.input-group input:focus {
  outline: none;
  border-color: #80bdff;
  box-shadow: 0 0 0 0.2rem rgba(0, 123, 255, 0.25);
}

.input-group input:disabled {
  background-color: #f8f9fa;
  color: #6c757d;
  cursor: not-allowed;
}

.input-group input::placeholder {
  color: #adb5bd;
}

/* Error Message */
.error-message {
  background-color: #f8d7da;
  color: #721c24;
  padding: 12px 16px;
  border: 1px solid #f5c6cb;
  border-radius: 6px;
  font-size: 14px;
  text-align: center;
  animation: slideDown 0.3s ease-out;
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.auth-btn {
  height: 40px;
  background-color: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s;
  margin-top: 10px;
}

.auth-btn:hover:not(:disabled) {
  background-color: #0056b3;
}

.auth-btn:disabled {
  background: #6c757d;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.auth-toggle {
  margin-top: 30px;
  text-align: center;
  border-top: 1px solid #e9ecef;
  padding-top: 25px;
}

.auth-toggle p {
  color: #6c757d;
  font-size: 14px;
  margin: 0;
}

.toggle-btn {
  background: none;
  border: none;
  color: #007bff;
  font-weight: 600;
  cursor: pointer;
  margin-left: 5px;
  padding: 2px;
  font-size: 14px;
  text-decoration: none;
  transition: color 0.2s;
}

.toggle-btn:hover:not(:disabled) {
  color: #0056b3;
  text-decoration: underline;
}

.toggle-btn:disabled {
  color: #6c757d;
  cursor: not-allowed;
}

/* Responsive Design */
@media (max-width: 480px) {
  .auth-container {
    padding: 10px;
  }
  
  .auth-card {
    padding: 25px;
    margin: 10px;
  }
  
  .auth-header h1 {
    font-size: 1.8rem;
  }
  
  .input-group input {
    height: 40px;
    font-size: 16px; /* Prevent zoom on iOS */
  }
  
  .auth-btn {
    height: 40px;
  }
}

/* Focus visible for accessibility */
.auth-btn:focus-visible,
.toggle-btn:focus-visible,
.input-group input:focus-visible {
  outline: 2px solid #007bff;
  outline-offset: 2px;
}
</style>
