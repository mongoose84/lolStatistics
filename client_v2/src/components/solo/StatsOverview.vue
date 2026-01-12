<template>
  <div class="stats-overview card">
    <div class="card-header">
      <h3 class="card-title">Performance Stats</h3>
    </div>

    <div v-if="loading" class="loading-skeleton">
      <div class="skeleton-row" v-for="i in 5" :key="i"></div>
    </div>

    <div v-else-if="stats" class="stats-content">
      <!-- Win/Loss Record -->
      <div class="stat-row">
        <span class="stat-name">Record</span>
        <span class="stat-value">
          <span class="wins">{{ stats.wins || 0 }}W</span>
          <span class="separator"> - </span>
          <span class="losses">{{ stats.losses || 0 }}L</span>
        </span>
      </div>

      <!-- Average Stats -->
      <div class="stat-row">
        <span class="stat-name">Avg Kills</span>
        <span class="stat-value">{{ formatStat(stats.avgKills) }}</span>
      </div>
      <div class="stat-row">
        <span class="stat-name">Avg Deaths</span>
        <span class="stat-value">{{ formatStat(stats.avgDeaths) }}</span>
      </div>
      <div class="stat-row">
        <span class="stat-name">Avg Assists</span>
        <span class="stat-value">{{ formatStat(stats.avgAssists) }}</span>
      </div>
      <div class="stat-row">
        <span class="stat-name">Avg CS/min</span>
        <span class="stat-value">{{ formatStat(stats.avgCsPerMin) }}</span>
      </div>
      <div class="stat-row">
        <span class="stat-name">Avg Vision Score</span>
        <span class="stat-value">{{ formatStat(stats.avgVisionScore) }}</span>
      </div>
      <div class="stat-row">
        <span class="stat-name">Avg Damage</span>
        <span class="stat-value">{{ formatNumber(stats.avgDamage) }}</span>
      </div>
      <div class="stat-row">
        <span class="stat-name">Avg Gold</span>
        <span class="stat-value">{{ formatNumber(stats.avgGold) }}</span>
      </div>

      <!-- Trend indicator if available -->
      <div v-if="trends?.winRateTrend" class="trend-section">
        <div class="trend-header">Win Rate Trend</div>
        <div class="trend-indicator" :class="getTrendClass(trends.winRateTrend)">
          <span class="trend-arrow">{{ getTrendArrow(trends.winRateTrend) }}</span>
          <span class="trend-value">{{ formatTrend(trends.winRateTrend) }}</span>
        </div>
      </div>
    </div>

    <div v-else class="empty-state">
      <p>No stats available</p>
    </div>
  </div>
</template>

<script setup>
defineProps({
  stats: {
    type: Object,
    default: null
  },
  trends: {
    type: Object,
    default: null
  },
  loading: {
    type: Boolean,
    default: false
  }
})

function formatStat(value) {
  if (value == null) return '0.0'
  return value.toFixed(1)
}

function formatNumber(value) {
  if (value == null) return '0'
  if (value >= 1000) {
    return (value / 1000).toFixed(1) + 'k'
  }
  return Math.round(value).toString()
}

function getTrendClass(trend) {
  if (trend > 0) return 'trend-up'
  if (trend < 0) return 'trend-down'
  return 'trend-neutral'
}

function getTrendArrow(trend) {
  if (trend > 0) return '↑'
  if (trend < 0) return '↓'
  return '→'
}

function formatTrend(trend) {
  if (trend == null) return ''
  const sign = trend > 0 ? '+' : ''
  return `${sign}${trend.toFixed(1)}%`
}
</script>

<style scoped>
.card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
}

.card-header {
  margin-bottom: var(--spacing-lg);
  padding-bottom: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
}

.card-title {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.stats-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.stat-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-xs) 0;
}

.stat-name {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.stat-value {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}

.wins { color: #22c55e; }
.losses { color: #ef4444; }
.separator { color: var(--color-text-secondary); }

.trend-section {
  margin-top: var(--spacing-md);
  padding-top: var(--spacing-md);
  border-top: 1px solid var(--color-border);
}

.trend-header {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-xs);
}

.trend-indicator {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-weight: var(--font-weight-semibold);
}

.trend-up { color: #22c55e; }
.trend-down { color: #ef4444; }
.trend-neutral { color: var(--color-text-secondary); }

.trend-arrow {
  font-size: var(--font-size-lg);
}

/* Loading skeleton */
.loading-skeleton {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.skeleton-row {
  height: 24px;
  background: linear-gradient(90deg, var(--color-surface-hover) 25%, var(--color-surface) 50%, var(--color-surface-hover) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: var(--radius-sm);
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.empty-state {
  text-align: center;
  padding: var(--spacing-xl);
  color: var(--color-text-secondary);
}
</style>

