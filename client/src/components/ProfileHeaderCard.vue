<template>
  <div class="profile-header-card">
    <!-- Profile Icon -->
    <div class="profile-icon-container">
      <img
        v-if="profileIconUrl"
        :src="profileIconUrl"
        :alt="`${riotId} profile icon`"
        class="profile-icon"
        @error="handleIconError"
      />
      <div v-else class="profile-icon-placeholder">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
          <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
        </svg>
      </div>
      <span v-if="summonerLevel" class="level-badge">{{ summonerLevel }}</span>
    </div>

    <!-- Identity & Stats -->
    <div class="profile-info">
      <div class="identity">
        <h2 class="riot-id">{{ riotId }}</h2>
        <span class="region-badge">{{ regionLabel }}</span>
      </div>

      <!-- Rank Badges (placeholder until rank data is available) -->
      <div class="rank-badges">
        <div class="rank-badge placeholder" title="Ranked Solo/Duo">
          <span class="rank-icon">🏆</span>
          <span class="rank-text">--</span>
        </div>
        <div class="rank-badge placeholder" title="Ranked Flex">
          <span class="rank-icon">👥</span>
          <span class="rank-text">--</span>
        </div>
      </div>

      <!-- Stats Row -->
      <div class="stats-row">
        <div class="stat">
          <span class="stat-value">--</span>
          <span class="stat-label">Win Rate</span>
        </div>
        <div class="stat-divider"></div>
        <div class="stat">
          <span class="stat-value">--</span>
          <span class="stat-label">Games</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'

const props = defineProps({
  gameName: {
    type: String,
    required: true
  },
  tagLine: {
    type: String,
    required: true
  },
  region: {
    type: String,
    required: true
  },
  profileIconId: {
    type: Number,
    default: null
  },
  summonerLevel: {
    type: Number,
    default: null
  }
})

const iconError = ref(false)

// Data Dragon version - could be fetched dynamically in the future
const ddVersion = '14.24.1'

const riotId = computed(() => `${props.gameName}#${props.tagLine}`)

const profileIconUrl = computed(() => {
  if (!props.profileIconId || iconError.value) return null
  return `https://ddragon.leagueoflegends.com/cdn/${ddVersion}/img/profileicon/${props.profileIconId}.png`
})

const regionLabels = {
  euw1: 'EUW',
  eun1: 'EUNE',
  na1: 'NA',
  kr: 'KR',
  jp1: 'JP',
  br1: 'BR',
  la1: 'LAN',
  la2: 'LAS',
  oc1: 'OCE',
  tr1: 'TR',
  ru: 'RU',
  ph2: 'PH',
  sg2: 'SG',
  th2: 'TH',
  tw2: 'TW',
  vn2: 'VN'
}

const regionLabel = computed(() => regionLabels[props.region] || props.region.toUpperCase())

function handleIconError() {
  iconError.value = true
}
</script>

<style scoped>
.profile-header-card {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
}

.profile-icon-container {
  position: relative;
  flex-shrink: 0;
}

.profile-icon {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  border: 3px solid var(--color-primary);
  object-fit: cover;
}

.profile-icon-placeholder {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  border: 3px solid var(--color-border);
  background: var(--color-elevated);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-text-secondary);
}

.profile-icon-placeholder svg {
  width: 40px;
  height: 40px;
}

.level-badge {
  position: absolute;
  bottom: -4px;
  right: -4px;
  background: var(--color-primary);
  color: white;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-bold);
  padding: 2px 8px;
  border-radius: 10px;
  min-width: 28px;
  text-align: center;
}

.profile-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.identity {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.riot-id {
  margin: 0;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  letter-spacing: var(--letter-spacing);
}

.region-badge {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  background: var(--color-elevated);
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  text-transform: uppercase;
}

.rank-badges {
  display: flex;
  gap: var(--spacing-sm);
}

.rank-badge {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 10px;
  background: var(--color-elevated);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-sm);
}

.rank-badge.placeholder {
  opacity: 0.5;
}

.rank-icon {
  font-size: 14px;
}

.rank-text {
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-medium);
}

.stats-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  margin-top: var(--spacing-xs);
}

.stat {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
}

.stat-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.stat-divider {
  width: 1px;
  height: 32px;
  background: var(--color-border);
}

/* Responsive adjustments */
@media (max-width: 640px) {
  .profile-header-card {
    flex-direction: column;
    text-align: center;
  }

  .identity {
    justify-content: center;
  }

  .rank-badges {
    justify-content: center;
  }

  .stats-row {
    justify-content: center;
  }
}
</style>

