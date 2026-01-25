<template>
  <div class="goals-page">
    <div class="page-container">
      <div class="page-header">
        <h1 class="page-title">Goals</h1>
      </div>
      <div class="page-content">
        <p class="placeholder-text">Goals feature coming soon...</p>
      </div>
    </div>
  </div>
</template>

<script setup>
// Goals page - to be implemented
</script>

<style scoped>
.goals-page {
  min-height: 100vh;
  padding: var(--spacing-2xl);
}

.page-container {
  max-width: 1400px;
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

.page-content {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-2xl);
  text-align: center;
}

.placeholder-text {
  font-size: var(--font-size-lg);
  color: var(--color-text-secondary);
}
</style>

