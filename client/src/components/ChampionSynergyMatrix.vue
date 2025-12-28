<template>
  <div class="champion-synergy-matrix">
    <div class="matrix-header">
      <h3>Champion Synergy Matrix</h3>
      <p class="subtitle">What should we pick together?</p>
    </div>

    <div v-if="loading" class="matrix-loading">Loading synergy data…</div>
    <div v-else-if="error" class="matrix-error">{{ error }}</div>
    <div v-else-if="!hasData" class="matrix-empty">Not enough duo games for synergy analysis yet.</div>
    
    <div v-else class="matrix-content">
      <!-- Filter Buttons -->
      <div class="filter-buttons">
        <button 
          v-for="filter in filters" 
          :key="filter.value"
          :class="['filter-btn', { active: selectedFilter === filter.value }]"
          @click="selectedFilter = filter.value">
          {{ filter.label }}
        </button>
      </div>

      <!-- Heatmap Table -->
      <div class="heatmap-container">
        <table class="heatmap-table">
          <thead>
            <tr>
              <th class="corner-cell"></th>
              <th v-for="champ in player2Champions" :key="'header-' + champ.championId" class="champion-header">
                {{ champ.championName }}
              </th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="champ1 in player1Champions" :key="'row-' + champ1.championId">
              <th class="champion-header">{{ champ1.championName }}</th>
              <td 
                v-for="champ2 in player2Champions" 
                :key="'cell-' + champ1.championId + '-' + champ2.championId"
                :class="['synergy-cell', getWinRateClass(getSynergyData(champ1.championId, champ2.championId))]"
                :title="getCellTooltip(champ1, champ2)">
                <div class="cell-content">
                  <div class="winrate">{{ getSynergyWinRate(champ1.championId, champ2.championId) }}</div>
                  <div class="games">{{ getSynergyGames(champ1.championId, champ2.championId) }}g</div>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import getChampionSynergy from '@/assets/getChampionSynergy.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true
  },
  gamers: {
    type: Array,
    required: true
  }
});

const loading = ref(false);
const error = ref(null);
const synergyData = ref(null);
const selectedFilter = ref('mostPlayed');

const filters = [
  { value: 'mostPlayed', label: 'Most Played' },
  { value: 'highestWR', label: 'Highest WR' },
  { value: 'biggestDelta', label: 'Biggest Δ' }
];

// Fetch synergy data
async function loadSynergyData() {
  loading.value = true;
  error.value = null;

  try {
    const data = await getChampionSynergy(props.userId);
    synergyData.value = data;
  } catch (e) {
    error.value = e?.message || 'Failed to load champion synergy data.';
    console.error('Error loading synergy data:', e);
  } finally {
    loading.value = false;
  }
}

const hasData = computed(() => {
  return synergyData.value?.synergies && synergyData.value.synergies.length > 0;
});

// Build synergy lookup map
const synergyMap = computed(() => {
  if (!hasData.value) return new Map();
  
  const map = new Map();
  synergyData.value.synergies.forEach(s => {
    const key = `${s.championId1}-${s.championId2}`;
    map.set(key, s);
  });
  return map;
});

// Get unique champions for each player
const player1Champions = computed(() => {
  if (!hasData.value) return [];
  
  const champMap = new Map();
  synergyData.value.synergies.forEach(s => {
    if (!champMap.has(s.championId1)) {
      champMap.set(s.championId1, {
        championId: s.championId1,
        championName: s.championName1,
        totalGames: 0
      });
    }
    champMap.get(s.championId1).totalGames += s.gamesPlayed;
  });
  
  const champs = Array.from(champMap.values());
  return sortChampions(champs);
});

const player2Champions = computed(() => {
  if (!hasData.value) return [];
  
  const champMap = new Map();
  synergyData.value.synergies.forEach(s => {
    if (!champMap.has(s.championId2)) {
      champMap.set(s.championId2, {
        championId: s.championId2,
        championName: s.championName2,
        totalGames: 0
      });
    }
    champMap.get(s.championId2).totalGames += s.gamesPlayed;
  });
  
  const champs = Array.from(champMap.values());
  return sortChampions(champs);
});

function sortChampions(champions) {
  // Limit to top champions based on filter
  const sorted = [...champions];

  if (selectedFilter.value === 'mostPlayed') {
    sorted.sort((a, b) => b.totalGames - a.totalGames);
  } else if (selectedFilter.value === 'highestWR') {
    // Will implement after we have WR data
    sorted.sort((a, b) => b.totalGames - a.totalGames);
  }

  // Limit to top 5 champions for readability
  return sorted.slice(0, 5);
}

function getSynergyData(championId1, championId2) {
  const key = `${championId1}-${championId2}`;
  return synergyMap.value.get(key) || null;
}

function getSynergyWinRate(championId1, championId2) {
  const data = getSynergyData(championId1, championId2);
  if (!data || data.gamesPlayed === 0) return '-';
  return `${data.winrate.toFixed(0)}%`;
}

function getSynergyGames(championId1, championId2) {
  const data = getSynergyData(championId1, championId2);
  return data ? data.gamesPlayed : 0;
}

function getWinRateClass(data) {
  if (!data || data.gamesPlayed === 0) return 'no-data';
  if (data.gamesPlayed < 3) return 'low-sample';
  if (data.winrate >= 60) return 'wr-excellent';
  if (data.winrate >= 55) return 'wr-good';
  if (data.winrate >= 50) return 'wr-neutral';
  if (data.winrate >= 45) return 'wr-bad';
  return 'wr-terrible';
}

function getCellTooltip(champ1, champ2) {
  const data = getSynergyData(champ1.championId, champ2.championId);
  if (!data) return `${champ1.championName} + ${champ2.championName}: No games`;
  return `${champ1.championName} + ${champ2.championName}\n${data.wins}W ${data.losses}L (${data.winrate.toFixed(1)}%)`;
}

onMounted(() => {
  loadSynergyData();
});

watch(() => props.userId, () => {
  loadSynergyData();
});
</script>

<style scoped>
.champion-synergy-matrix {
  background: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.matrix-header {
  margin-bottom: 16px;
}

.matrix-header h3 {
  margin: 0 0 4px 0;
  font-size: 18px;
  font-weight: 600;
  color: #1a1a1a;
}

.subtitle {
  margin: 0;
  font-size: 13px;
  color: #666;
}

.matrix-loading,
.matrix-error,
.matrix-empty {
  padding: 40px;
  text-align: center;
  color: #666;
}

.matrix-error {
  color: #d32f2f;
}

.filter-buttons {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
}

.filter-btn {
  padding: 6px 12px;
  border: 1px solid #ddd;
  background: white;
  border-radius: 4px;
  cursor: pointer;
  font-size: 13px;
  transition: all 0.2s;
}

.filter-btn:hover {
  background: #f5f5f5;
}

.filter-btn.active {
  background: #1976d2;
  color: white;
  border-color: #1976d2;
}

.heatmap-container {
  overflow-x: auto;
}

.heatmap-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
}

.corner-cell {
  background: #f5f5f5;
  border: 1px solid #ddd;
}

.champion-header {
  background: #f5f5f5;
  padding: 8px;
  text-align: center;
  font-weight: 600;
  border: 1px solid #ddd;
  font-size: 11px;
  min-width: 70px;
}

.synergy-cell {
  padding: 4px;
  text-align: center;
  border: 1px solid #ddd;
  cursor: help;
  transition: transform 0.1s;
}

.synergy-cell:hover {
  transform: scale(1.05);
  z-index: 1;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.cell-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}

.winrate {
  font-weight: 600;
  font-size: 13px;
}

.games {
  font-size: 10px;
  color: #666;
}

/* Win rate color classes */
.no-data {
  background: #f5f5f5;
  color: #999;
}

.low-sample {
  background: #fff9e6;
  color: #666;
}

.wr-excellent {
  background: #4caf50;
  color: white;
}

.wr-good {
  background: #8bc34a;
  color: white;
}

.wr-neutral {
  background: #ffc107;
  color: #333;
}

.wr-bad {
  background: #ff9800;
  color: white;
}

.wr-terrible {
  background: #f44336;
  color: white;
}
</style>


