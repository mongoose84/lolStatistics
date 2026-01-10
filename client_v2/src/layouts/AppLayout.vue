<template>
  <div class="app-layout">
    <AppHeader />
    <main class="app-main">
      <router-view />
    </main>
  </div>
</template>

<script setup>
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import AppHeader from '../components/AppHeader.vue';
import { useAuthStore } from '../stores/authStore';

const router = useRouter();
const authStore = useAuthStore();

onMounted(async () => {
  await authStore.initialize();
  
  // Redirect if not authenticated
  if (!authStore.isAuthenticated) {
    router.push('/auth?mode=login');
    return;
  }
  
  // Redirect to verify if not verified
  if (!authStore.isVerified) {
    router.push('/auth/verify');
  }
});
</script>

<style scoped>
.app-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-main {
  flex: 1;
  padding-top: 64px; /* Height of AppHeader */
}
</style>

