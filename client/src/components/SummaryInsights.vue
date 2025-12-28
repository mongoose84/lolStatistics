<template>
  <div class="summary-insights">
    <div v-if="loading" class="insights-loading">Loading insights…</div>
    <div v-else-if="error" class="insights-error">{{ error }}</div>
    <div v-else-if="!hasData" class="insights-empty">Not enough data for insights.</div>

    <ChartCard v-else title="📊 Summary Insights">
      <div class="insights-content">
        <p class="insights-subtitle">Key performance highlights and cross-server comparison</p>
        
        <div class="insights-grid">
          <div 
            v-for="(insight, index) in insights" 
            :key="index" 
            class="insight-card"
            :class="insight.type"
          >
            <div class="insight-icon">{{ insight.icon }}</div>
            <div class="insight-body">
              <h4 class="insight-title">{{ insight.title }}</h4>
              <p class="insight-description">{{ insight.description }}</p>
              <div v-if="insight.action" class="insight-action">
                <span class="action-label">💡 Goal:</span>
                <span class="action-text">{{ insight.action }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getComparison from '@/assets/getComparison.js';
import getMatchDuration from '@/assets/getMatchDuration.js';
import getChampionPerformance from '@/assets/getChampionPerformance.js';

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
        description: `Your CS/min is ${cs1.toFixed(1)}. Improving your farming will significantly boost your gold income.`,
        action: `Practice last-hitting to reach ${csTarget}+ CS/min`
      });
    } else {
      allInsights.push({
        priority: 5,
        icon: '🌾',
        title: 'Farm Efficiency',
        type: 'positive',
        description: `Strong CS/min at ${cs1.toFixed(1)}. You're farming efficiently and maintaining good gold income.`,
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
        description: `${higherCS} has ${csDiff.toFixed(1)} more CS/min than ${lowerCS} (${higherVal.toFixed(1)} vs ${lowerVal.toFixed(1)}). This suggests more consistent farming patterns.`,
        action: `Match ${higherCS}'s farming discipline on ${lowerCS}`
      });
    } else {
      allInsights.push({
        priority: 3,
        icon: '🌾',
        title: 'Consistent Farming',
        type: 'positive',
        description: `Similar CS/min across both servers (${cs1.toFixed(1)} vs ${cs2.toFixed(1)}). Your farming is consistent.`,
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
        description: `Averaging ${deaths1.toFixed(1)} deaths per game. Reducing deaths will improve your impact and winrate.`,
        action: `Focus on positioning and map awareness to reach <${deathTarget} deaths/game`
      });
    } else {
      allInsights.push({
        priority: 6,
        icon: '💀',
        title: 'Death Prevention',
        type: 'positive',
        description: `Low death average at ${deaths1.toFixed(1)} per game. You're playing safely and avoiding unnecessary risks.`,
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
        title: 'Safer Play on One Server',
        type: 'comparison',
        description: `${lowerDeaths} has ${deathDiff.toFixed(1)} fewer deaths than ${higherDeaths} (${lowerVal.toFixed(1)} vs ${higherVal.toFixed(1)}). This indicates more cautious positioning.`,
        action: `Apply ${lowerDeaths}'s safer playstyle to ${higherDeaths}`
      });
    } else {
      allInsights.push({
        priority: 4,
        icon: '💀',
        title: 'Consistent Safety',
        type: 'positive',
        description: `Similar death rates across servers (${deaths1.toFixed(1)} vs ${deaths2.toFixed(1)}). Your risk management is consistent.`,
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
        description: `Excellent ${wr1.toFixed(1)}% winrate! You're climbing effectively and winning more than you lose.`,
        action: null
      });
    } else if (wr1 >= 50) {
      allInsights.push({
        priority: 4,
        icon: '🏆',
        title: 'Balanced Winrate',
        type: 'neutral',
        description: `${wr1.toFixed(1)}% winrate shows you're holding your own. Small improvements can push you higher.`,
        action: 'Focus on consistency to push above 55%'
      });
    } else {
      allInsights.push({
        priority: (50 - wr1) * 0.5,
        icon: '🏆',
        title: 'Winrate Improvement',
        type: 'improvement',
        description: `${wr1.toFixed(1)}% winrate suggests room for growth. Focus on your weakest areas to turn more losses into wins.`,
        action: 'Target 50%+ winrate by reducing deaths and improving CS'
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
        description: `${higherWR} has ${wrDiff.toFixed(1)}% higher winrate than ${lowerWR} (${higherVal.toFixed(1)}% vs ${lowerVal.toFixed(1)}%). Different meta or playstyle may be factors.`,
        action: `Analyze what works on ${higherWR} and apply it to ${lowerWR}`
      });
    } else {
      allInsights.push({
        priority: 3,
        icon: '🏆',
        title: 'Consistent Performance',
        type: 'positive',
        description: `Similar winrates across servers (${wr1.toFixed(1)}% vs ${wr2.toFixed(1)}%). Your skill translates well across regions.`,
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
        description: `Outstanding ${kda1.toFixed(1)} KDA! You're getting kills and assists while staying alive.`,
        action: null
      });
    } else if (kda1 >= 2.5) {
      allInsights.push({
        priority: 3,
        icon: '⚔️',
        title: 'Solid Combat Stats',
        type: 'neutral',
        description: `${kda1.toFixed(1)} KDA is respectable. Continue participating in fights while staying safe.`,
        action: 'Push for 3.0+ KDA by reducing risky plays'
      });
    } else {
      allInsights.push({
        priority: (3 - kda1) * 5,
        icon: '⚔️',
        title: 'Combat Improvement Needed',
        type: 'improvement',
        description: `${kda1.toFixed(1)} KDA suggests you're dying too often relative to your kills and assists.`,
        action: 'Focus on safer positioning and better fight selection to reach 2.5+ KDA'
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
        title: 'Combat Effectiveness Gap',
        type: 'comparison',
        description: `${higherKDA} shows better combat stats with ${kdaDiff.toFixed(1)} higher KDA (${higherVal.toFixed(1)} vs ${lowerVal.toFixed(1)}). More efficient trading and teamfighting.`,
        action: `Study your ${higherKDA} replays to improve ${lowerKDA} performance`
      });
    } else {
      allInsights.push({
        priority: 3,
        icon: '⚔️',
        title: 'Consistent Combat',
        type: 'positive',
        description: `Similar KDA across servers (${kda1.toFixed(1)} vs ${kda2.toFixed(1)}). Your fighting style is consistent.`,
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
        description: `${gold1.toFixed(0)} gold/min can be improved. More gold means faster item spikes and stronger impact.`,
        action: `Improve farming and objective participation to reach ${goldTarget}+ gold/min`
      });
    } else {
      allInsights.push({
        priority: 5,
        icon: '💰',
        title: 'Gold Efficiency',
        type: 'positive',
        description: `Strong ${gold1.toFixed(0)} gold/min. You're efficiently converting time into gold through farming and objectives.`,
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
        title: 'Gold Income Difference',
        type: 'comparison',
        description: `${higherGold} generates ${goldDiff.toFixed(0)} more gold/min than ${lowerGold} (${higherVal.toFixed(0)} vs ${lowerVal.toFixed(0)}). Better farming or objective control.`,
        action: `Match ${higherGold}'s farming efficiency on ${lowerGold}`
      });
    } else {
      allInsights.push({
        priority: 3,
        icon: '💰',
        title: 'Consistent Gold Income',
        type: 'positive',
        description: `Similar gold generation across servers (${gold1.toFixed(0)} vs ${gold2.toFixed(0)}). Your economic play is stable.`,
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
        title: 'Flexible Champion Pool',
        type: 'positive',
        description: `Playing ${totalChamps} different champions with ${champsWithMultipleGames} played frequently. Good adaptability to team needs.`,
        action: null
      });
    } else if (totalChamps < 5) {
      allInsights.push({
        priority: 3,
        icon: '🎮',
        title: 'Limited Champion Pool',
        type: 'improvement',
        description: `Only ${totalChamps} champions played. Expanding your pool can help in more team compositions.`,
        action: 'Learn 2-3 more champions to increase flexibility'
      });
    } else {
      allInsights.push({
        priority: 2,
        icon: '🎮',
        title: 'Moderate Champion Pool',
        type: 'neutral',
        description: `${totalChamps} champions played. Consider mastering a few more for better draft flexibility.`,
        action: 'Add 1-2 comfort picks to your rotation'
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
      const duration = bestBucket.label;
      allInsights.push({
        priority: 5,
        icon: '⏱️',
        title: 'Game Duration Strength',
        type: 'positive',
        description: `${bestWR.toFixed(0)}% winrate in ${duration} games. You excel at this game pace.`,
        action: `Try to steer games toward ${duration} through your playstyle`
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
  margin-top: 2rem;
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

.insights-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.insights-subtitle {
  margin: 0;
  font-size: 0.9rem;
  color: var(--color-text-muted);
  text-align: center;
}

.insights-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1rem;
}

.insight-card {
  display: flex;
  gap: 1rem;
  padding: 1.25rem;
  background: var(--color-bg);
  border: 2px solid var(--color-border);
  border-radius: 12px;
  transition: all 0.3s ease;
}

.insight-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.insight-card.positive {
  border-color: var(--color-success);
  background: linear-gradient(135deg, var(--color-bg) 0%, rgba(34, 197, 94, 0.05) 100%);
}

.insight-card.improvement {
  border-color: var(--color-warning);
  background: linear-gradient(135deg, var(--color-bg) 0%, rgba(245, 158, 11, 0.05) 100%);
}

.insight-card.comparison {
  border-color: var(--color-primary);
  background: linear-gradient(135deg, var(--color-bg) 0%, rgba(139, 92, 246, 0.05) 100%);
}

.insight-card.neutral {
  border-color: var(--color-border);
}

.insight-icon {
  font-size: 2rem;
  flex-shrink: 0;
  line-height: 1;
  margin-top: 0.2rem;
}

.insight-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  min-width: 0;
}

.insight-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: var(--color-text);
  line-height: 1.3;
}

.insight-description {
  margin: 0;
  font-size: 0.9rem;
  line-height: 1.5;
  color: var(--color-text);
}

.insight-action {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  padding: 0.75rem;
  background: var(--color-bg-elev);
  border-radius: 6px;
  border-left: 3px solid var(--color-primary);
  margin-top: 0.25rem;
}

.action-label {
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--color-primary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.action-text {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-text);
  line-height: 1.4;
}

/* Responsive */
@media (max-width: 768px) {
  .insights-grid {
    grid-template-columns: 1fr;
  }
}
</style>


