<template>
  <div class="user-settings-page">
    <div class="page-container">
      <div class="page-header">
        <h1 class="page-title">User Settings</h1>
      </div>
      
      <div class="settings-content">
        <!-- Account Section -->
        <div class="settings-section">
          <h2 class="section-title">Account</h2>
          <div class="settings-card">
            <div class="setting-item">
              <div class="setting-info">
                <span class="setting-label">Username</span>
                <span class="setting-value">{{ username }}</span>
              </div>
            </div>
            <div class="setting-item">
              <div class="setting-info">
                <span class="setting-label">Email</span>
                <span class="setting-value">{{ email }}</span>
              </div>
            </div>
            <div class="setting-item">
              <div class="setting-info">
                <span class="setting-label">Tier</span>
                <span class="setting-value tier-badge" :class="`tier-${tier}`">{{ tierLabel }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Logout Section -->
        <div class="settings-section">
          <h2 class="section-title">Session</h2>
          <div class="settings-card">
            <button @click="handleLogout" class="btn-logout" :disabled="isLoggingOut">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
                <path fill-rule="evenodd" d="M3 4.25A2.25 2.25 0 015.25 2h5.5A2.25 2.25 0 0113 4.25v2a.75.75 0 01-1.5 0v-2a.75.75 0 00-.75-.75h-5.5a.75.75 0 00-.75.75v11.5c0 .414.336.75.75.75h5.5a.75.75 0 00.75-.75v-2a.75.75 0 011.5 0v2A2.25 2.25 0 0110.75 18h-5.5A2.25 2.25 0 013 15.75V4.25z" clip-rule="evenodd" />
                <path fill-rule="evenodd" d="M19 10a.75.75 0 00-.75-.75H8.704l1.048-.943a.75.75 0 10-1.004-1.114l-2.5 2.25a.75.75 0 000 1.114l2.5 2.25a.75.75 0 101.004-1.114l-1.048-.943h9.546A.75.75 0 0019 10z" clip-rule="evenodd" />
              </svg>
              {{ isLoggingOut ? 'Logging out...' : 'Logout' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { trackAuth } from '../services/analyticsApi';

const router = useRouter();
const authStore = useAuthStore();

const isLoggingOut = ref(false);

const username = computed(() => authStore.username || 'User');
const email = computed(() => authStore.email || 'Not set');
const tier = computed(() => authStore.tier || 'free');

const tierLabel = computed(() => {
  const t = tier.value;
  if (t === 'pro') return 'Pro';
  if (t === 'premium') return 'Premium';
  return 'Free';
});

async function handleLogout() {
  isLoggingOut.value = true;
  try {
    await authStore.logout();
    trackAuth('logout', true);
    router.push('/');
  } catch (e) {
    console.error('Logout failed:', e);
    trackAuth('logout', false);
    // Still redirect even if logout fails
    router.push('/');
  } finally {
    isLoggingOut.value = false;
  }
}
</script>

<style scoped>
.user-settings-page {
  min-height: 100vh;
  padding: var(--spacing-2xl);
}

.page-container {
  max-width: 800px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: var(--spacing-2xl);
}

.page-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  letter-spacing: var(--letter-spacing);
}

.settings-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2xl);
}

.settings-section {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.section-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  letter-spacing: var(--letter-spacing);
}

.settings-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-xl);
}

.setting-item {
  padding: var(--spacing-md) 0;
  border-bottom: 1px solid var(--color-border);
}

.setting-item:last-child {
  border-bottom: none;
}

.setting-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.setting-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
}

.setting-value {
  font-size: var(--font-size-sm);
  color: var(--color-text);
}

.tier-badge {
  padding: 4px 12px;
  border-radius: var(--radius-sm);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  font-size: var(--font-size-xs);
  letter-spacing: 0.05em;
}

.tier-free {
  background: rgba(136, 136, 136, 0.2);
  color: #888888;
}

.tier-premium {
  background: rgba(59, 130, 246, 0.2);
  color: #3b82f6;
}

.tier-pro {
  background: rgba(109, 40, 217, 0.2);
  color: var(--color-primary);
}

.btn-logout {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-md) var(--spacing-lg);
  background: transparent;
  border: 1px solid rgba(239, 68, 68, 0.3);
  border-radius: var(--radius-md);
  color: #ef4444;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-logout:hover:not(:disabled) {
  background: rgba(239, 68, 68, 0.1);
  border-color: #ef4444;
}

.btn-logout:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-icon {
  width: 20px;
  height: 20px;
}
</style>

