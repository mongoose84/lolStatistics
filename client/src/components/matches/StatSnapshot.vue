<template>
  <div class="stat-snapshot">
    <h3 class="section-title">Stats</h3>
    <div class="stats-grid">
      <div class="stat-item" v-for="stat in stats" :key="stat.label">
        <div class="stat-header">
          <span class="stat-label">{{ stat.label }}</span>
          <span v-if="stat.trend" class="trend-arrow" :class="stat.trend">
            {{ stat.trend === 'up' ? '↑' : stat.trend === 'down' ? '↓' : '' }}
          </span>
        </div>
        <span class="stat-value">{{ stat.value }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  match: {
    type: Object,
    required: true
  },
  baseline: {
    type: Object,
    default: null
  }
})

const stats = computed(() => {
  const m = props.match
  const b = props.baseline

  const formatKda = () => {
    const kda = m.deaths === 0 ? (m.kills + m.assists) : (m.kills + m.assists) / m.deaths
    return kda.toFixed(2)
  }

  const getTrend = (value, avgValue, threshold = 0.1) => {
    if (!b || b.gamesCount === 0) return null
    const diff = (value - avgValue) / avgValue
    if (diff >= threshold) return 'up'
    if (diff <= -threshold) return 'down'
    return null
  }

  return [
    {
      label: 'KDA',
      value: `${m.kills}/${m.deaths}/${m.assists}`,
      trend: null
    },
    {
      label: 'KDA Ratio',
      value: formatKda(),
      trend: b ? getTrend(parseFloat(formatKda()), b.avgKda, 0.15) : null
    },
    {
      label: 'Damage Dealt',
      value: formatNumber(m.damageDealt),
      trend: b ? getTrend(m.damageDealt, b.avgDamageDealt, 0.15) : null
    },
    {
      label: 'Damage Taken',
      value: formatNumber(m.damageTaken),
      trend: null
    },
    {
      label: 'CS',
      value: m.creepScore.toString(),
      trend: null
    },
    {
      label: 'CS/min',
      value: m.csPerMin.toFixed(1),
      trend: b ? getTrend(m.csPerMin, b.avgCsPerMin, 0.1) : null
    },
    {
      label: 'Gold',
      value: formatNumber(m.goldEarned),
      trend: b ? getTrend(m.goldEarned, b.avgGoldEarned, 0.1) : null
    },
    {
      label: 'Gold/min',
      value: m.goldPerMin.toFixed(0),
      trend: b ? getTrend(m.goldPerMin, b.avgGoldPerMin, 0.1) : null
    },
    {
      label: 'Vision Score',
      value: m.visionScore.toString(),
      trend: b ? getTrend(m.visionScore, b.avgVisionScore, 0.15) : null
    },
    {
      label: 'Kill Part.',
      value: `${m.killParticipation.toFixed(0)}%`,
      trend: b ? getTrend(m.killParticipation, b.avgKillParticipation, 0.1) : null
    }
  ]
})

function formatNumber(num) {
  if (num >= 1000) {
    return (num / 1000).toFixed(1) + 'k'
  }
  return num.toString()
}
</script>

<style scoped>
.stat-snapshot {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.section-title {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--spacing-sm);
}

.stat-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: var(--spacing-sm);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
}

.stat-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.stat-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.trend-arrow {
  font-size: 10px;
  font-weight: var(--font-weight-bold);
}

.trend-arrow.up {
  color: #22c55e;
}

.trend-arrow.down {
  color: #ef4444;
}

.stat-value {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}
</style>

