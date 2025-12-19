<template>
  <div class="statistics-card" role="region" aria-label="Aggregated win rate">
    <div class="title">Overall</div>
    <div class="content">
      <div
        class="chart"
        role="img"
        :aria-label="`Wins ${wins}, Losses ${losses}`"
        :style="{ width: size + 'px', height: size + 'px' }"
      >
        <svg :width="size" :height="size" :viewBox="`0 0 ${size} ${size}`">
          <g :transform="`rotate(-90 ${center} ${center})`">
            <circle
              :cx="center" :cy="center" :r="radius"
              :stroke="lossColor" :stroke-width="strokeWidth"
              fill="none"
            />
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
      <div class="summary">{{ total }}G {{ wins }}W {{ losses }}L</div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, onMounted, watch } from 'vue'
import getOverallStats from '@/assets/getOverallStats.js'

const props = defineProps({
  userId: { type: [String, Number], required: true }
})

const size = 120
const strokeWidth = 12
const radius = (size - strokeWidth) / 2
const center = size / 2

const loading = ref(false)
const error = ref(null)
const gamesPlayed = ref(0)
const winsValue = ref(0)

async function load() {
  if (props.userId === undefined || props.userId === null || props.userId === '') return
  loading.value = true
  error.value = null
  try {
    const data = await getOverallStats(props.userId)
    gamesPlayed.value = Number(data?.gamesPlayed ?? 0)
    winsValue.value = Number(data?.wins ?? 0)
  } catch (e) {
    error.value = e?.message || 'Failed to load overall stats'
    gamesPlayed.value = 0
    winsValue.value = 0
  } finally {
    loading.value = false
  }
}

onMounted(load)
watch(() => props.userId, load)

const wins = computed(() => winsValue.value)
const total = computed(() => gamesPlayed.value)
const losses = computed(() => Math.max(0, total.value - wins.value))

const winRate = computed(() => {
  if (!total.value) return 0
  return Math.round((wins.value / total.value) * 100)
})

const circumference = 2 * Math.PI * radius
const dashArray = computed(() => {
  if (!total.value) return `0 ${circumference}`
  const winLen = (wins.value / total.value) * circumference
  const rest = Math.max(0, circumference - winLen)
  return `${winLen} ${rest}`
})

const winColor = 'var(--color-success)'
const lossColor = 'var(--color-danger)'
</script>

<style scoped>
.statistics-card {
  width: 100%;
  height: calc(var(--card-width) * 0.975); /* ~60% of GamerCard height */
  box-sizing: border-box;
  padding: 0.75rem 1rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background-color: var(--color-bg-elev);
  color: var(--color-text);
  display: block;
}

.title {
  font-weight: 600;
  font-size: 1.05rem;
  margin: 0.25rem 0 0 1rem; /* top-left corner */
}

.content {
  margin-top: 0.5rem;
}

.chart {
  position: relative;
  margin-top: 1.6rem;
  margin-left: 3.5rem; /* left align with ~5rem margin */
}

.summary {
  margin-left: 3.5rem;
  margin-top: 0.6rem;
  font-size: 0.95rem;
  opacity: 0.9;
}

.chart-label {
  position: absolute;
  left: 50%;
  top: 50%;
  transform: translate(-50%, -50%);
  font-size: 0.9rem;
  color: var(--color-text);
  text-align: center;
  pointer-events: none;
}
</style>
