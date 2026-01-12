<template>
  <div class="champion-matchups card">
    <div class="card-header">
      <h3 class="card-title">Champion Performance</h3>
      <span v-if="matchups?.length" class="matchup-count">{{ matchups.length }} champions</span>
    </div>

    <div v-if="loading" class="loading-skeleton">
      <div class="skeleton-row" v-for="i in 5" :key="i"></div>
    </div>

    <div v-else-if="matchups && matchups.length" class="matchups-content">
      <!-- Table Header -->
      <div class="matchups-header">
        <span class="col-champion">Champion</span>
        <span class="col-games">Games</span>
        <span class="col-winrate">Win Rate</span>
        <span class="col-kda">KDA</span>
        <span class="col-cs">CS/min</span>
      </div>

      <!-- Champion Rows -->
      <div 
        v-for="matchup in displayedMatchups" 
        :key="matchup.championId"
        class="matchup-row"
      >
        <div class="col-champion">
          <img 
            :src="getChampionIcon(matchup.championName)"
            :alt="matchup.championName"
            class="champion-icon"
          />
          <span class="champion-name">{{ matchup.championName }}</span>
        </div>
        <span class="col-games">{{ matchup.games }}</span>
        <span class="col-winrate" :class="getWinrateClass(matchup.winRate)">
          {{ formatPercent(matchup.winRate) }}
        </span>
        <span class="col-kda">{{ formatKDA(matchup.kda) }}</span>
        <span class="col-cs">{{ formatStat(matchup.csPerMin) }}</span>
      </div>

      <!-- Show More Button -->
      <button 
        v-if="matchups.length > displayLimit && !showAll"
        class="btn-show-more"
        @click="showAll = true"
      >
        Show All ({{ matchups.length - displayLimit }} more)
      </button>
    </div>

    <div v-else class="empty-state">
      <p>No champion data available</p>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  matchups: {
    type: Array,
    default: () => []
  },
  loading: {
    type: Boolean,
    default: false
  }
})

const showAll = ref(false)
const displayLimit = 10

const displayedMatchups = computed(() => {
  if (!props.matchups) return []
  const sorted = [...props.matchups].sort((a, b) => b.games - a.games)
  return showAll.value ? sorted : sorted.slice(0, displayLimit)
})

function getChampionIcon(championName) {
  if (!championName) return ''
  // Normalize champion name for Data Dragon (remove spaces, special chars)
  const normalized = championName.replace(/[^a-zA-Z]/g, '')
  return `https://ddragon.leagueoflegends.com/cdn/14.24.1/img/champion/${normalized}.png`
}

function getWinrateClass(winRate) {
  if (winRate >= 55) return 'winrate-high'
  if (winRate >= 50) return 'winrate-good'
  return 'winrate-low'
}

function formatPercent(value) {
  if (value == null) return '0%'
  return `${Math.round(value)}%`
}

function formatKDA(kda) {
  if (kda == null) return '0.00'
  return kda.toFixed(2)
}

function formatStat(value) {
  if (value == null) return '0.0'
  return value.toFixed(1)
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
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-lg);
  padding-bottom: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
}

.card-title {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.matchup-count {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.matchups-header, .matchup-row {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr 1fr 1fr;
  gap: var(--spacing-sm);
  align-items: center;
  padding: var(--spacing-sm) 0;
}

.matchups-header {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-medium);
  border-bottom: 1px solid var(--color-border);
}

.matchup-row {
  font-size: var(--font-size-sm);
  border-bottom: 1px solid var(--color-border);
}

.matchup-row:last-of-type {
  border-bottom: none;
}

.matchup-row:hover {
  background: var(--color-surface-hover);
}

.col-champion {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.champion-icon {
  width: 32px;
  height: 32px;
  border-radius: var(--radius-sm);
  object-fit: cover;
}

.champion-name {
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}

.col-games, .col-kda, .col-cs {
  color: var(--color-text-secondary);
  text-align: center;
}

.col-winrate {
  text-align: center;
  font-weight: var(--font-weight-medium);
}

.winrate-high { color: #22c55e; }
.winrate-good { color: #3b82f6; }
.winrate-low { color: #ef4444; }

.btn-show-more {
  width: 100%;
  padding: var(--spacing-sm);
  margin-top: var(--spacing-md);
  background: var(--color-surface-hover);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-show-more:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

/* Loading skeleton */
.loading-skeleton {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.skeleton-row {
  height: 48px;
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

@media (max-width: 600px) {
  .matchups-header, .matchup-row {
    grid-template-columns: 2fr 1fr 1fr;
  }

  .col-kda, .col-cs {
    display: none;
  }
}
</style>

