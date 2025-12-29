<template>
  <div class="summary-insights">
    <h3 class="insights-header">📊 Summary Insights</h3>
    <p class="insights-subtitle">Key performance highlights and actionable goals</p>

    <div v-if="loading" class="insights-loading">Loading insights…</div>
    <div v-else-if="error" class="insights-error">{{ error }}</div>
    <div v-else-if="!hasData" class="insights-empty">Not enough data for insights.</div>

    <div v-else class="insights-row">
      <div
        v-for="(insight, index) in insights"
        :key="index"
        class="insight-card"
        :class="insight.type"
      >
        <!-- Icon at top (like profile picture) -->
        <div class="insight-icon-wrap">
          <div class="insight-icon" :class="insight.type">{{ insight.icon }}</div>
        </div>

        <!-- Type label (like level) -->
        <div class="insight-type-label">{{ getTypeLabel(insight.type) }}</div>

        <!-- Title (like gamer name) -->
        <div class="insight-title">{{ insight.title }}</div>

        <!-- Visual indicator (like win/loss chart) -->
        <div class="insight-visual">
          <div class="visual-bar" :class="insight.type">
            <div class="visual-fill" :style="{ width: insight.visualPercent + '%' }"></div>
          </div>
          <div class="visual-label">{{ insight.visualLabel }}</div>
        </div>

        <!-- Main stat (like game info) -->
        <div class="insight-stat">{{ insight.stat }}</div>

        <!-- Description (like KDA) -->
        <div class="insight-description">{{ insight.description }}</div>

        <!-- Action/Goal (like per-minute stats) -->
        <div v-if="insight.action" class="insight-action">
          💡 {{ insight.action }}
        </div>
        <div v-else class="insight-action positive">
          ✓ Keep it up!
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { getComparison } from '@/api/solo.js';
import { getMatchDuration } from '@/api/solo.js';
import { getChampionPerformance } from '@/api/solo.js';

// Helper to get type label
function getTypeLabel(type) {
  switch (type) {
    case 'positive': return 'Strength';
    case 'improvement': return 'Focus Area';
    case 'comparison': return 'Server Gap';
    case 'neutral': return 'Balanced';
    default: return 'Insight';
  }
}

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const comparisonData = ref(null);
const durationData = ref(null);
const championData = ref(null);

const hasData = computed(() => {
  return comparisonData.value && 
         comparisonData.value.winrate && 
         comparisonData.value.winrate.length > 0;
});

// Helper to get server name from gamer name
function getServer(gamerName) {
  const parts = gamerName.split('#');
  return parts.length > 1 ? parts[1] : 'Server';
}

// Helper to calculate percentage difference
function percentDiff(val1, val2) {
  if (val2 === 0) return 0;
  return ((val1 - val2) / val2) * 100;
}

// Generate 5 key insights
const insights = computed(() => {
  if (!hasData.value) return [];

  const results = [];
  const gamers = comparisonData.value.winrate || [];
  const isSingleServer = gamers.length === 1;

  // Get data for each server
  const gamer1 = gamers[0];
  const gamer2 = gamers.length > 1 ? gamers[1] : null;

  const server1 = getServer(gamer1.gamerName);
  const server2 = gamer2 ? getServer(gamer2.gamerName) : null;

  // Extract metrics for gamer1
  const wr1 = comparisonData.value.winrate?.find(g => g.gamerName === gamer1.gamerName)?.value || 0;
  const cs1 = comparisonData.value.csPrMin?.find(g => g.gamerName === gamer1.gamerName)?.value || 0;
  const deaths1 = comparisonData.value.avgDeaths?.find(g => g.gamerName === gamer1.gamerName)?.value || 0;
  const gold1 = comparisonData.value.goldPrMin?.find(g => g.gamerName === gamer1.gamerName)?.value || 0;
  const kda1 = comparisonData.value.kda?.find(g => g.gamerName === gamer1.gamerName)?.value || 0;
  const kills1 = comparisonData.value.avgKills?.find(g => g.gamerName === gamer1.gamerName)?.value || 0;
  const assists1 = comparisonData.value.avgAssists?.find(g => g.gamerName === gamer1.gamerName)?.value || 0;

  // Extract metrics for gamer2 (if exists)
  let wr2, cs2, deaths2, gold2, kda2, kills2, assists2;
  if (gamer2) {
    wr2 = comparisonData.value.winrate?.find(g => g.gamerName === gamer2.gamerName)?.value || 0;
    cs2 = comparisonData.value.csPrMin?.find(g => g.gamerName === gamer2.gamerName)?.value || 0;
    deaths2 = comparisonData.value.avgDeaths?.find(g => g.gamerName === gamer2.gamerName)?.value || 0;
    gold2 = comparisonData.value.goldPrMin?.find(g => g.gamerName === gamer2.gamerName)?.value || 0;
    kda2 = comparisonData.value.kda?.find(g => g.gamerName === gamer2.gamerName)?.value || 0;
    kills2 = comparisonData.value.avgKills?.find(g => g.gamerName === gamer2.gamerName)?.value || 0;
    assists2 = comparisonData.value.avgAssists?.find(g => g.gamerName === gamer2.gamerName)?.value || 0;
  }

  // Calculate insights based on available data
  const allInsights = [];

  // INSIGHT 1: CS/min comparison or highlight
  if (isSingleServer) {
    const csTarget = cs1 < 4 ? 5 : cs1 < 5.5 ? 6.5 : 7.5;
    if (cs1 < csTarget) {
      allInsights.push({
        priority: (csTarget - cs1) * 10,
        icon: '🌾',
        title: 'Farm Efficiency',
        type: 'improvement',
        stat: `${cs1.toFixed(1)} CS/min`,
        visualPercent: Math.min((cs1 / csTarget) * 100, 100),
        visualLabel: `${Math.round((cs1 / csTarget) * 100)}% of target`,
        description: `Below optimal farming rate`,
        action: `Reach ${csTarget}+ CS/min`
      });
    } else {
      allInsights.push({
        priority: 5,
        icon: '🌾',
        title: 'Farm Efficiency',
        type: 'positive',
        stat: `${cs1.toFixed(1)} CS/min`,
        visualPercent: 100,
        visualLabel: 'Excellent',
        description: `Strong farming consistency`,
        action: null
      });
    }
  } else {
    const csDiff = Math.abs(cs1 - cs2);
    const higherCS = cs1 > cs2 ? server1 : server2;
    const lowerCS = cs1 > cs2 ? server2 : server1;
    const higherVal = Math.max(cs1, cs2);
    const lowerVal = Math.min(cs1, cs2);

    if (csDiff > 0.5) {
      allInsights.push({
        priority: csDiff * 8,
        icon: '🌾',
        title: 'Farm Efficiency Gap',
        type: 'comparison',
        stat: `${higherVal.toFixed(1)} vs ${lowerVal.toFixed(1)}`,
        visualPercent: (lowerVal / higherVal) * 100,
        visualLabel: `${csDiff.toFixed(1)} CS/min gap`,
        description: `${higherCS} farms better`,
        action: `Match ${higherCS} on ${lowerCS}`
      });
    } else {
      allInsights.push({
        priority: 3,
        icon: '🌾',
        title: 'Consistent Farming',
        type: 'positive',
        stat: `${cs1.toFixed(1)} CS/min`,
        visualPercent: 100,
        visualLabel: 'Balanced',
        description: `Similar across servers`,
        action: null
      });
    }
  }

  // INSIGHT 2: Deaths comparison or highlight
  if (isSingleServer) {
    const deathTarget = deaths1 > 8 ? 6 : deaths1 > 6 ? 5 : 4;
    if (deaths1 > deathTarget) {
      allInsights.push({
        priority: (deaths1 - deathTarget) * 12,
        icon: '💀',
        title: 'Death Prevention',
        type: 'improvement',
        stat: `${deaths1.toFixed(1)} deaths/game`,
        visualPercent: Math.max(100 - ((deaths1 - deathTarget) / deathTarget) * 100, 0),
        visualLabel: `${(deaths1 - deathTarget).toFixed(1)} above target`,
        description: `Too many deaths`,
        action: `Reduce to <${deathTarget}/game`
      });
    } else {
      allInsights.push({
        priority: 6,
        icon: '💀',
        title: 'Death Prevention',
        type: 'positive',
        stat: `${deaths1.toFixed(1)} deaths/game`,
        visualPercent: 100,
        visualLabel: 'Excellent',
        description: `Safe positioning`,
        action: null
      });
    }
  } else {
    const deathDiff = Math.abs(deaths1 - deaths2);
    const lowerDeaths = deaths1 < deaths2 ? server1 : server2;
    const higherDeaths = deaths1 < deaths2 ? server2 : server1;
    const lowerVal = Math.min(deaths1, deaths2);
    const higherVal = Math.max(deaths1, deaths2);

    if (deathDiff > 0.8) {
      allInsights.push({
        priority: deathDiff * 10,
        icon: '💀',
        title: 'Safety Gap',
        type: 'comparison',
        stat: `${lowerVal.toFixed(1)} vs ${higherVal.toFixed(1)}`,
        visualPercent: (lowerVal / higherVal) * 100,
        visualLabel: `${deathDiff.toFixed(1)} fewer on ${lowerDeaths}`,
        description: `${lowerDeaths} plays safer`,
        action: `Copy ${lowerDeaths} style`
      });
    } else {
      allInsights.push({
        priority: 4,
        icon: '💀',
        title: 'Consistent Safety',
        type: 'positive',
        stat: `${deaths1.toFixed(1)} deaths/game`,
        visualPercent: 100,
        visualLabel: 'Balanced',
        description: `Similar risk management`,
        action: null
      });
    }
  }

  // INSIGHT 3: Winrate comparison or highlight
  if (isSingleServer) {
    if (wr1 >= 55) {
      allInsights.push({
        priority: 7,
        icon: '🏆',
        title: 'Strong Winrate',
        type: 'positive',
        stat: `${wr1.toFixed(1)}% WR`,
        visualPercent: Math.min(wr1, 100),
        visualLabel: 'Climbing',
        description: `Winning consistently`,
        action: null
      });
    } else if (wr1 >= 50) {
      allInsights.push({
        priority: 4,
        icon: '🏆',
        title: 'Balanced Winrate',
        type: 'neutral',
        stat: `${wr1.toFixed(1)}% WR`,
        visualPercent: wr1,
        visualLabel: 'Even',
        description: `Holding steady`,
        action: 'Push to 55%+'
      });
    } else {
      allInsights.push({
        priority: (50 - wr1) * 0.5,
        icon: '🏆',
        title: 'Winrate Growth',
        type: 'improvement',
        stat: `${wr1.toFixed(1)}% WR`,
        visualPercent: wr1,
        visualLabel: `${(50 - wr1).toFixed(1)}% below 50%`,
        description: `Room to improve`,
        action: 'Target 50%+ WR'
      });
    }
  } else {
    const wrDiff = Math.abs(wr1 - wr2);
    const higherWR = wr1 > wr2 ? server1 : server2;
    const lowerWR = wr1 > wr2 ? server2 : server1;
    const higherVal = Math.max(wr1, wr2);
    const lowerVal = Math.min(wr1, wr2);

    if (wrDiff > 5) {
      allInsights.push({
        priority: wrDiff * 0.8,
        icon: '🏆',
        title: 'Winrate Variance',
        type: 'comparison',
        stat: `${higherVal.toFixed(1)}% vs ${lowerVal.toFixed(1)}%`,
        visualPercent: (lowerVal / higherVal) * 100,
        visualLabel: `${wrDiff.toFixed(1)}% gap`,
        description: `${higherWR} performs better`,
        action: `Study ${higherWR} games`
      });
    } else {
      allInsights.push({
        priority: 3,
        icon: '🏆',
        title: 'Consistent WR',
        type: 'positive',
        stat: `${wr1.toFixed(1)}% WR`,
        visualPercent: Math.max(wr1, wr2),
        visualLabel: 'Balanced',
        description: `Similar across servers`,
        action: null
      });
    }
  }

  // INSIGHT 4: KDA and combat effectiveness
  if (isSingleServer) {
    if (kda1 >= 3.5) {
      allInsights.push({
        priority: 6,
        icon: '⚔️',
        title: 'Combat Excellence',
        type: 'positive',
        stat: `${kda1.toFixed(1)} KDA`,
        visualPercent: Math.min((kda1 / 5) * 100, 100),
        visualLabel: 'Outstanding',
        description: `Efficient teamfighting`,
        action: null
      });
    } else if (kda1 >= 2.5) {
      allInsights.push({
        priority: 3,
        icon: '⚔️',
        title: 'Solid Combat',
        type: 'neutral',
        stat: `${kda1.toFixed(1)} KDA`,
        visualPercent: (kda1 / 5) * 100,
        visualLabel: 'Good',
        description: `Respectable stats`,
        action: 'Push for 3.0+ KDA'
      });
    } else {
      allInsights.push({
        priority: (3 - kda1) * 5,
        icon: '⚔️',
        title: 'Combat Focus',
        type: 'improvement',
        stat: `${kda1.toFixed(1)} KDA`,
        visualPercent: (kda1 / 3) * 100,
        visualLabel: `${(3 - kda1).toFixed(1)} below target`,
        description: `Dying too often`,
        action: 'Reach 2.5+ KDA'
      });
    }
  } else {
    const kdaDiff = Math.abs(kda1 - kda2);
    const higherKDA = kda1 > kda2 ? server1 : server2;
    const lowerKDA = kda1 > kda2 ? server2 : server1;
    const higherVal = Math.max(kda1, kda2);
    const lowerVal = Math.min(kda1, kda2);

    if (kdaDiff > 0.5) {
      allInsights.push({
        priority: kdaDiff * 6,
        icon: '⚔️',
        title: 'Combat Gap',
        type: 'comparison',
        stat: `${higherVal.toFixed(1)} vs ${lowerVal.toFixed(1)}`,
        visualPercent: (lowerVal / higherVal) * 100,
        visualLabel: `${kdaDiff.toFixed(1)} KDA gap`,
        description: `${higherKDA} fights better`,
        action: `Study ${higherKDA} replays`
      });
    } else {
      allInsights.push({
        priority: 3,
        icon: '⚔️',
        title: 'Consistent Combat',
        type: 'positive',
        stat: `${kda1.toFixed(1)} KDA`,
        visualPercent: Math.min((Math.max(kda1, kda2) / 5) * 100, 100),
        visualLabel: 'Balanced',
        description: `Similar fighting style`,
        action: null
      });
    }
  }

  // INSIGHT 5: Gold efficiency
  if (isSingleServer) {
    const goldTarget = gold1 < 300 ? 350 : gold1 < 380 ? 420 : 450;
    if (gold1 < goldTarget) {
      allInsights.push({
        priority: (goldTarget - gold1) * 0.08,
        icon: '💰',
        title: 'Gold Generation',
        type: 'improvement',
        stat: `${gold1.toFixed(0)} G/min`,
        visualPercent: (gold1 / goldTarget) * 100,
        visualLabel: `${Math.round((gold1 / goldTarget) * 100)}% of target`,
        description: `Can farm more gold`,
        action: `Reach ${goldTarget}+ G/min`
      });
    } else {
      allInsights.push({
        priority: 5,
        icon: '💰',
        title: 'Gold Efficiency',
        type: 'positive',
        stat: `${gold1.toFixed(0)} G/min`,
        visualPercent: 100,
        visualLabel: 'Excellent',
        description: `Strong gold income`,
        action: null
      });
    }
  } else {
    const goldDiff = Math.abs(gold1 - gold2);
    const higherGold = gold1 > gold2 ? server1 : server2;
    const lowerGold = gold1 > gold2 ? server2 : server1;
    const higherVal = Math.max(gold1, gold2);
    const lowerVal = Math.min(gold1, gold2);

    if (goldDiff > 30) {
      allInsights.push({
        priority: goldDiff * 0.15,
        icon: '💰',
        title: 'Gold Income Gap',
        type: 'comparison',
        stat: `${higherVal.toFixed(0)} vs ${lowerVal.toFixed(0)}`,
        visualPercent: (lowerVal / higherVal) * 100,
        visualLabel: `${goldDiff.toFixed(0)} G/min gap`,
        description: `${higherGold} earns more`,
        action: `Match ${higherGold} income`
      });
    } else {
      allInsights.push({
        priority: 3,
        icon: '💰',
        title: 'Consistent Gold',
        type: 'positive',
        stat: `${gold1.toFixed(0)} G/min`,
        visualPercent: 100,
        visualLabel: 'Balanced',
        description: `Similar gold income`,
        action: null
      });
    }
  }

  // INSIGHT 6: Champion pool flexibility (if data available)
  if (championData.value?.champions) {
    const totalChamps = championData.value.champions.length;
    const champsWithMultipleGames = championData.value.champions.filter(c =>
      c.servers.some(s => s.gamesPlayed >= 3)
    ).length;

    if (totalChamps >= 10 && champsWithMultipleGames >= 5) {
      allInsights.push({
        priority: 4,
        icon: '🎮',
        title: 'Flexible Pool',
        type: 'positive',
        stat: `${totalChamps} champions`,
        visualPercent: Math.min((champsWithMultipleGames / 8) * 100, 100),
        visualLabel: `${champsWithMultipleGames} mastered`,
        description: `Great adaptability`,
        action: null
      });
    } else if (totalChamps < 5) {
      allInsights.push({
        priority: 3,
        icon: '🎮',
        title: 'Limited Pool',
        type: 'improvement',
        stat: `${totalChamps} champions`,
        visualPercent: (totalChamps / 8) * 100,
        visualLabel: 'Narrow',
        description: `Expand champion pool`,
        action: 'Learn 2-3 more champs'
      });
    } else {
      allInsights.push({
        priority: 2,
        icon: '🎮',
        title: 'Moderate Pool',
        type: 'neutral',
        stat: `${totalChamps} champions`,
        visualPercent: (totalChamps / 10) * 100,
        visualLabel: 'Decent',
        description: `Room to expand`,
        action: 'Add 1-2 comfort picks'
      });
    }
  }

  // INSIGHT 7: Game duration preference (if data available)
  if (durationData.value?.gamers && durationData.value.gamers.length > 0) {
    const gamer = durationData.value.gamers[0];
    const buckets = gamer.buckets || [];

    // Find best performing duration bucket
    let bestBucket = null;
    let bestWR = 0;
    buckets.forEach(b => {
      if (b.gamesPlayed >= 3 && b.winrate > bestWR) {
        bestWR = b.winrate;
        bestBucket = b;
      }
    });

    if (bestBucket && bestWR >= 55) {
      const duration = bestBucket.durationRange || 'Unknown';
      allInsights.push({
        priority: 5,
        icon: '⏱️',
        title: 'Duration Strength',
        type: 'positive',
        stat: `${bestWR.toFixed(0)}% WR`,
        visualPercent: bestWR,
        visualLabel: `${duration} min`,
        description: `Excel at this pace`,
        action: `Steer to ${duration} min`
      });
    }
  }

  // Sort by priority and return top 5
  allInsights.sort((a, b) => b.priority - a.priority);
  return allInsights.slice(0, 5);
});

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    // Load all data in parallel
    const [comparison, duration, champions] = await Promise.all([
      getComparison(props.userId),
      getMatchDuration(props.userId).catch(() => null),
      getChampionPerformance(props.userId).catch(() => null)
    ]);

    comparisonData.value = comparison;
    durationData.value = duration;
    championData.value = champions;
  } catch (e) {
    console.error('Error loading summary insights:', e);
    error.value = e?.message || 'Failed to load insights.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.summary-insights {
  width: 100%;
  margin-top: 2.5rem;
  padding: 0 1rem;
}

.insights-header {
  margin: 0 0 0.5rem 0;
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-text);
  text-align: center;
}

.insights-subtitle {
  margin: 0 0 1.5rem 0;
  font-size: 0.9rem;
  color: var(--color-text-muted);
  text-align: center;
}

.insights-loading,
.insights-error,
.insights-empty {
  text-align: center;
  padding: 2rem;
  color: var(--color-text-muted);
}

.insights-error {
  color: var(--color-danger);
}

/* Row of insight cards - matching GamerCardsList layout */
.insights-row {
  display: flex;
  gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
  max-width: 100%;
}

/* Individual insight card - matching GamerCard style */
.insight-card {
  width: 240px;
  min-height: 380px;
  padding: 1.25rem;
  background: var(--color-bg-elev);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  color: var(--color-text);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  transition: all 0.2s ease;
}

.insight-card:hover {
  background-color: var(--color-bg-hover);
  transform: translateY(-4px);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
}

/* Border colors based on type */
.insight-card.positive {
  border-color: var(--color-success);
  border-width: 2px;
}

.insight-card.improvement {
  border-color: var(--color-warning);
  border-width: 2px;
}

.insight-card.comparison {
  border-color: var(--color-primary);
  border-width: 2px;
}

.insight-card.neutral {
  border-color: var(--color-border);
}

/* Icon wrap - matching profile icon style */
.insight-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
}

.insight-icon {
  width: 72px;
  height: 72px;
  border-radius: 8px;
  border: 1px solid var(--color-border);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 2.5rem;
  background: var(--color-bg);
}

.insight-icon.positive {
  background: linear-gradient(135deg, var(--color-bg) 0%, rgba(34, 197, 94, 0.1) 100%);
  border-color: var(--color-success);
}

.insight-icon.improvement {
  background: linear-gradient(135deg, var(--color-bg) 0%, rgba(245, 158, 11, 0.1) 100%);
  border-color: var(--color-warning);
}

.insight-icon.comparison {
  background: linear-gradient(135deg, var(--color-bg) 0%, rgba(139, 92, 246, 0.1) 100%);
  border-color: var(--color-primary);
}

/* Type label - matching level style */
.insight-type-label {
  margin-top: 0.25rem;
  font-size: 0.8rem;
  opacity: 0.8;
  font-weight: 500;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

/* Title - matching gamer name style */
.insight-title {
  margin-top: 0.2rem;
  font-weight: 600;
  font-size: 0.95rem;
  text-align: center;
}

/* Visual indicator - matching win/loss chart */
.insight-visual {
  margin-top: 0.4rem;
  width: 140px;
  height: 120px;
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.visual-bar {
  width: 100%;
  height: 14px;
  background: var(--color-bg);
  border-radius: 7px;
  overflow: hidden;
  border: 1px solid var(--color-border);
  position: relative;
}

.visual-fill {
  height: 100%;
  transition: width 0.5s ease;
  border-radius: 6px;
}

.insight-card.positive .visual-fill {
  background: var(--color-success);
}

.insight-card.improvement .visual-fill {
  background: var(--color-warning);
}

.insight-card.comparison .visual-fill {
  background: var(--color-primary);
}

.insight-card.neutral .visual-fill {
  background: var(--color-text-muted);
}

.visual-label {
  margin-top: 0.5rem;
  font-size: 0.75rem;
  color: var(--color-text);
  text-align: center;
  font-weight: 600;
}

/* Main stat - matching game info style */
.insight-stat {
  margin-top: 0.6rem;
  font-size: 0.95rem;
  font-weight: 600;
  opacity: 0.95;
}

/* Description - matching KDA style */
.insight-description {
  margin-top: 0.8rem;
  font-size: 0.9rem;
  font-weight: 500;
  text-align: center;
  line-height: 1.3;
}

/* Action - matching per-minute style */
.insight-action {
  margin-top: 0.8rem;
  font-size: 0.85rem;
  opacity: 0.9;
  text-align: center;
  padding: 0.5rem 0.75rem;
  background: var(--color-bg);
  border-radius: 6px;
  line-height: 1.4;
}

.insight-action.positive {
  color: var(--color-success);
  font-weight: 600;
}

/* Responsive */
@media (max-width: 1200px) {
  .insights-row {
    justify-content: center;
  }
}

@media (max-width: 768px) {
  .insight-card {
    width: 220px;
    min-height: 360px;
  }
}
</style>


