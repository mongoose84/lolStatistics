<template>
  <div class="gamer-card" :aria-label="`${gamer.gamerName} #${gamer.tagline}`">
    <div class="icon-wrap">
      <img
        class="icon"
        :src="gamer.iconUrl"
        :alt="`${gamer.gamerName} profile icon`"
        width="72"
        height="72"
        loading="lazy"
      />
    </div>

    <div class="level">Level {{ gamer.level }}</div>

    <div class="name">
      {{ gamer.gamerName }}<span class="tag">#{{ gamer.tagline }}</span>
    </div>

    <div
      class="chart"
      role="img"
      :aria-label="`Wins ${wins}, Losses ${losses}`"
      :style="{ width: size + 'px', height: size + 'px' }"
    >
      <svg :width="size" :height="size" :viewBox="`0 0 ${size} ${size}`">
        <g :transform="`rotate(-90 ${center} ${center})`">
          <!-- Losses ring (background) -->
          <circle
            :cx="center" :cy="center" :r="radius"
            :stroke="lossColor" :stroke-width="strokeWidth"
            fill="none"
          />
          <!-- Wins arc -->
          <circle
            :cx="center" :cy="center" :r="radius"
            :stroke="winColor" :stroke-width="strokeWidth"
            fill="none"
            :stroke-dasharray="dashArray"
            stroke-linecap="butt"
          />
        </g>
      </svg>
      <div class="chart-label">{{ winRate }}%</div>
    </div>

    <div class="game-info">{{ total }}G {{ wins }}W {{ losses }}L</div>
    <div class="kda">{{ avgKills }} / {{ avgDeaths }} / {{ avgAssists }}</div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  gamer: {
    type: Object,
    required: true
  }
})

const size = 120
const strokeWidth = 12
const radius = (size - strokeWidth) / 2
const center = size / 2

const stats = computed(() => props.gamer?.stats ?? {})

const total = computed(() => Number(stats.value?.totalMatches ?? 0))

const circumference = 2 * Math.PI * radius
const wins = computed(() => Number(stats.value?.wins ?? 0))
const losses = computed(() => Math.max(0, total.value - wins.value))

const winRate = computed(() => {
  if (!total.value) return 0
  return Math.round((wins.value / total.value) * 100)
})

const dashArray = computed(() => {
  if (!total.value) return `0 ${circumference}`
  const winLen = (wins.value / total.value) * circumference
  const rest = Math.max(0, circumference - winLen)
  return `${winLen} ${rest}`
})

const winColor = 'var(--color-success)'
const lossColor = 'var(--color-danger)'

const avgKills = computed(() => {
  const tk = Number(stats.value?.totalKills ?? 0)
  if (!total.value) return '0.0'
  return (tk / total.value).toFixed(1)
})

const avgDeaths = computed(() => {
  const td = Number(stats.value?.totalDeaths ?? 0)
  if (!total.value) return '0.0'
  return (td / total.value).toFixed(1)
})

const avgAssists = computed(() => {
  const ta = Number(stats.value?.totalAssists ?? 0)
  if (!total.value) return '0.0'
  return (ta / total.value).toFixed(1)
})
</script>

<style scoped>
.gamer-card {
  width: 260px;
  aspect-ratio: 64 / 89; /* playing card proportion */
  box-sizing: border-box;
  padding: 0.75rem 0.75rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background-color: var(--color-bg-elev);
  color: var(--color-text);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  transition: background-color 0.15s ease;
}

.gamer-card:hover {
  background-color: var(--color-bg-hover);
}

.icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
}

.icon {
  width: 72px;
  height: 72px;
  border-radius: 8px;
  border: 1px solid var(--color-border);
  object-fit: cover;
  background: var(--color-bg);
}

.level {
  margin-top: 0.25rem;
  font-size: 0.95rem;
  opacity: 0.9;
}

.name {
  margin-top: 0.2rem;
  font-weight: 600;
}

.tag {
  margin-left: 0.35rem;
  opacity: 0.8;
  font-weight: 500;
}

.chart {
  margin-top: 0.4rem;
  position: relative;
}

.chart-label {
  position: absolute;
  left: 50%;
  top: 50%;
  transform: translate(-50%, -50%);
  font-size: 0.85rem;
  color: var(--color-text);
  text-align: center;
  pointer-events: none;
}

.game-info {
  margin-top: 0.35rem;
  font-size: 0.9rem;
  opacity: 0.9;
}

.kda {
  margin-top: 0.25rem;
  font-size: 0.95rem;
  font-weight: 500;
}
</style>