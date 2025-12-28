<template>
  <div class="duo-vs-enemy-matrix">
    <div class="matrix-header">
      <h3>Duo vs Enemy Champions</h3>
      <p class="subtitle">Draft prep for ranked & clash</p>
    </div>

    <div v-if="loading" class="matrix-loading">Loading matchup data…</div>
    <div v-else-if="error" class="matrix-error">{{ error }}</div>
    <div v-else-if="!hasData" class="matrix-empty">Not enough duo games for matchup analysis yet.</div>
    
    <div v-else class="matrix-content">
      <!-- Duo Champion Selector -->
      <div class="duo-selector">
        <label>Select Duo Combo:</label>
        <select v-model="selectedDuoCombo" class="duo-combo-select">
          <option v-for="combo in duoCombos" :key="combo.key" :value="combo.key">
            {{ combo.label }} ({{ combo.games }}g, {{ combo.winrate }}%)
          </option>
        </select>
      </div>

      <!-- Enemy Lane Selector -->
      <div class="lane-selector">
        <button 
          v-for="lane in availableLanes" 
          :key="lane"
          :class="['lane-btn', { active: selectedLane === lane }]"
          @click="selectedLane = lane">
          {{ lane }}
        </button>
      </div>

      <!-- Heatmap Table -->
      <div v-if="selectedDuoCombo && selectedLane" class="heatmap-container">
        <table class="heatmap-table">
          <thead>
            <tr>
              <th class="corner-cell">vs {{ selectedLane }}</th>
              <th class="winrate-header">Win Rate</th>
              <th class="games-header">Games</th>
            </tr>
          </thead>
          <tbody>
            <tr 
              v-for="enemy in enemyChampions" 
              :key="enemy.championId"
              :class="['enemy-row', getWinRateClass(enemy)]">
              <td class="champion-name">{{ enemy.championName }}</td>
              <td class="winrate-cell">
                <div class="winrate-bar-container">
                  <div class="winrate-bar" :style="{ width: enemy.winrate + '%' }"></div>
                  <span class="winrate-text">{{ enemy.winrate.toFixed(0) }}%</span>
                </div>
              </td>
              <td class="games-cell">{{ enemy.gamesPlayed }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import getDuoVsEnemy from '@/assets/getDuoVsEnemy.js';

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
const matchupData = ref(null);
const selectedDuoCombo = ref(null);
const selectedLane = ref(null);

// Fetch matchup data
async function loadMatchupData() {
  loading.value = true;
  error.value = null;

  try {
    const data = await getDuoVsEnemy(props.userId);
    matchupData.value = data;
    
    // Auto-select first duo combo and lane
    if (duoCombos.value.length > 0) {
      selectedDuoCombo.value = duoCombos.value[0].key;
    }
    if (availableLanes.value.length > 0) {
      selectedLane.value = availableLanes.value[0];
    }
  } catch (e) {
    error.value = e?.message || 'Failed to load duo vs enemy data.';
    console.error('Error loading matchup data:', e);
  } finally {
    loading.value = false;
  }
}

const hasData = computed(() => {
  return matchupData.value?.matchups && matchupData.value.matchups.length > 0;
});

// Get unique duo combinations
const duoCombos = computed(() => {
  if (!hasData.value) return [];
  
  const comboMap = new Map();
  matchupData.value.matchups.forEach(m => {
    const key = `${m.duoChampionId1}-${m.duoChampionId2}`;
    if (!comboMap.has(key)) {
      comboMap.set(key, {
        key,
        label: `${m.duoChampionName1} + ${m.duoChampionName2}`,
        championId1: m.duoChampionId1,
        championName1: m.duoChampionName1,
        championId2: m.duoChampionId2,
        championName2: m.duoChampionName2,
        games: 0,
        wins: 0
      });
    }
    const combo = comboMap.get(key);
    combo.games += m.gamesPlayed;
    combo.wins += m.wins;
  });
  
  const combos = Array.from(comboMap.values());
  combos.forEach(c => {
    c.winrate = c.games > 0 ? ((c.wins / c.games) * 100).toFixed(0) : 0;
  });
  
  // Sort by games played
  combos.sort((a, b) => b.games - a.games);
  return combos;
});

// Get available lanes for selected duo combo
const availableLanes = computed(() => {
  if (!hasData.value || !selectedDuoCombo.value) return [];
  
  const lanes = new Set();
  matchupData.value.matchups
    .filter(m => `${m.duoChampionId1}-${m.duoChampionId2}` === selectedDuoCombo.value)
    .forEach(m => lanes.add(m.enemyLane));
  
  return Array.from(lanes).sort();
});

// Get enemy champions for selected duo combo and lane
const enemyChampions = computed(() => {
  if (!hasData.value || !selectedDuoCombo.value || !selectedLane.value) return [];

  const enemies = matchupData.value.matchups
    .filter(m =>
      `${m.duoChampionId1}-${m.duoChampionId2}` === selectedDuoCombo.value &&
      m.enemyLane === selectedLane.value
    )
    .map(m => ({
      championId: m.enemyChampionId,
      championName: m.enemyChampionName,
      gamesPlayed: m.gamesPlayed,
      wins: m.wins,
      losses: m.losses,
      winrate: m.winrate
    }));

  // Sort by games played
  enemies.sort((a, b) => b.gamesPlayed - a.gamesPlayed);

  // Limit to top 10 for readability
  return enemies.slice(0, 10);
});

function getWinRateClass(enemy) {
  if (enemy.gamesPlayed < 3) return 'low-sample';
  if (enemy.winrate >= 60) return 'wr-excellent';
  if (enemy.winrate >= 55) return 'wr-good';
  if (enemy.winrate >= 50) return 'wr-neutral';
  if (enemy.winrate >= 45) return 'wr-bad';
  return 'wr-terrible';
}

onMounted(() => {
  loadMatchupData();
});

watch(() => props.userId, () => {
  loadMatchupData();
});
</script>

<style scoped>
.duo-vs-enemy-matrix {
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

.duo-selector {
  margin-bottom: 16px;
}

.duo-selector label {
  display: block;
  margin-bottom: 6px;
  font-size: 13px;
  font-weight: 500;
  color: #333;
}

.duo-combo-select {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 13px;
  background: white;
  cursor: pointer;
}

.lane-selector {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}

.lane-btn {
  padding: 6px 12px;
  border: 1px solid #ddd;
  background: white;
  border-radius: 4px;
  cursor: pointer;
  font-size: 12px;
  transition: all 0.2s;
}

.lane-btn:hover {
  background: #f5f5f5;
}

.lane-btn.active {
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
  font-size: 13px;
}

.corner-cell,
.winrate-header,
.games-header {
  background: #f5f5f5;
  padding: 10px;
  text-align: left;
  font-weight: 600;
  border: 1px solid #ddd;
  font-size: 12px;
}

.winrate-header {
  width: 60%;
}

.games-header {
  width: 15%;
  text-align: center;
}

.enemy-row {
  transition: background 0.2s;
}

.enemy-row:hover {
  background: #f9f9f9;
}

.champion-name {
  padding: 10px;
  font-weight: 500;
  border: 1px solid #ddd;
}

.winrate-cell {
  padding: 10px;
  border: 1px solid #ddd;
}

.winrate-bar-container {
  position: relative;
  height: 24px;
  background: #f0f0f0;
  border-radius: 4px;
  overflow: hidden;
}

.winrate-bar {
  position: absolute;
  left: 0;
  top: 0;
  height: 100%;
  transition: width 0.3s, background 0.3s;
}

.winrate-text {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  font-weight: 600;
  font-size: 12px;
  color: #333;
  z-index: 1;
}

.games-cell {
  padding: 10px;
  text-align: center;
  border: 1px solid #ddd;
  color: #666;
}

/* Win rate color classes for rows */
.wr-excellent .winrate-bar {
  background: #4caf50;
}

.wr-good .winrate-bar {
  background: #8bc34a;
}

.wr-neutral .winrate-bar {
  background: #ffc107;
}

.wr-bad .winrate-bar {
  background: #ff9800;
}

.wr-terrible .winrate-bar {
  background: #f44336;
}

.low-sample .winrate-bar {
  background: #e0e0e0;
}

.low-sample .champion-name {
  opacity: 0.7;
}
</style>
