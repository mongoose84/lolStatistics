<template>
  <div class="overview-page">
    <div class="overview-container">
      <div class="page-header">
        <h1 class="page-title">Overview</h1>
      </div>

      <!-- Overview components will be implemented here (G14b-G14g) -->
      <div class="overview-content">
        <!-- Placeholder for future implementation -->
      </div>
    </div>
  </div>
</template>

<script setup>
// Overview page - ready for implementation (G14a-G14g)
// Components to be added:
// - OverviewPlayerHeader (G14b)
// - RankSnapshot (G14c)
// - LastMatchCard (G14d)
// - GoalProgressPreview (G14e)
// - SuggestedActions (G14f)
// - OverviewLayout (G14g)
</script>

<style scoped>
.overview-page {
  min-height: 100vh;
  padding: var(--spacing-2xl);
}

.overview-container {
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

.overview-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xl);
}
</style>

