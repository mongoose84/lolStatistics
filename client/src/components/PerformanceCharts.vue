<template>
  <div class="performance-charts">
    <div class="charts-header">
      <h3>Performance Over Time</h3>
      <div class="limit-toggle">
        <button 
          v-for="opt in limitOptions" 
          :key="opt"
          :class="['limit-btn', { active: limit === opt }]"
          @click="limit = opt"
        >
          {{ opt }} games
        </button>
      </div>
    </div>

    <div v-if="loading" class="charts-loading">Loading performance data…</div>
    <div v-else-if="error" class="charts-error">{{ error }}</div>
    <div v-else-if="!hasData" class="charts-empty">No performance data available yet.</div>
    
    <div v-else class="charts-grid">
      <!-- Winrate Chart -->
      <div class="chart-card">
        <h4>Winrate Over Time</h4>
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="line-chart" aria-label="Winrate chart">
          <!-- Grid lines -->
          <g class="grid">
            <line v-for="y in yGridLines" :key="'wr-grid-' + y" 
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="y" :y2="y" />
          </g>
          <!-- Y-axis labels -->
          <g class="y-labels">
            <text v-for="(label, i) in yLabelsWinrate" :key="'wr-y-' + i"
              :x="padding.left - 8" :y="yGridLines[i]" text-anchor="end" dominant-baseline="middle">
              {{ label }}%
            </text>
          </g>
          <!-- Lines for each gamer -->
          <g v-for="(gamer, gi) in chartData" :key="'wr-line-' + gi">
            <polyline 
              :points="getWinratePoints(gamer)"
              :stroke="getColor(gi)"
              fill="none"
              stroke-width="2"
              stroke-linejoin="round"
            />
          </g>
          <!-- Legend -->
          <g class="legend" :transform="`translate(${padding.left}, ${chartHeight - 10})`">
            <g v-for="(gamer, gi) in chartData" :key="'wr-leg-' + gi" 
              :transform="`translate(${gi * 120}, 0)`">
              <rect :fill="getColor(gi)" width="12" height="12" rx="2" />
              <text x="16" y="10" class="legend-text">{{ gamer.shortName }}</text>
            </g>
          </g>
        </svg>
      </div>

      <!-- Gold/CS Chart -->
      <div class="chart-card">
        <h4>Gold/min & CS/min</h4>
        <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="line-chart" aria-label="Economy chart">
          <!-- Grid lines -->
          <g class="grid">
            <line v-for="y in yGridLinesEcon" :key="'econ-grid-' + y" 
              :x1="padding.left" :x2="chartWidth - padding.right"
              :y1="y" :y2="y" />
          </g>
          <!-- Y-axis labels (left = gold, right = CS) -->
          <g class="y-labels">
            <text v-for="(label, i) in yLabelsGold" :key="'gold-y-' + i"
              :x="padding.left - 8" :y="yGridLinesEcon[i]" text-anchor="end" dominant-baseline="middle">
              {{ label }}
            </text>
          </g>
          <!-- Gold lines (solid) -->
          <g v-for="(gamer, gi) in chartData" :key="'gold-line-' + gi">
            <polyline 
              :points="getGoldPoints(gamer)"
              :stroke="getColor(gi)"
              fill="none"
              stroke-width="2"
              stroke-linejoin="round"
            />
          </g>
          <!-- CS lines (dashed) -->
          <g v-for="(gamer, gi) in chartData" :key="'cs-line-' + gi">
            <polyline 
              :points="getCsPoints(gamer)"
              :stroke="getColor(gi)"
              fill="none"
              stroke-width="2"
              stroke-dasharray="4 3"
              stroke-linejoin="round"
              opacity="0.7"
            />
          </g>
          <!-- Legend -->
          <g class="legend" :transform="`translate(${padding.left}, ${chartHeight - 10})`">
            <g v-for="(gamer, gi) in chartData" :key="'econ-leg-' + gi" 
              :transform="`translate(${gi * 120}, 0)`">
              <line :stroke="getColor(gi)" x1="0" y1="6" x2="12" y2="6" stroke-width="2" />
              <line :stroke="getColor(gi)" x1="14" y1="6" x2="26" y2="6" stroke-width="2" stroke-dasharray="4 3" opacity="0.7" />
              <text x="30" y="10" class="legend-text">{{ gamer.shortName }}</text>
            </g>
          </g>
        </svg>
        <div class="chart-footnote">Solid = Gold/min, Dashed = CS/min</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import getPerformance from '@/assets/getPerformance.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const limitOptions = [20, 50, 100];
const limit = ref(100);
const loading = ref(false);
const error = ref(null);
const performanceData = ref(null);

// Chart dimensions
const chartWidth = 400;
const chartHeight = 200;
const padding = { top: 20, right: 20, bottom: 30, left: 45 };

const colors = [
  'var(--color-primary)',      // Purple
  'var(--color-success)',      // Green
  '#f59e0b',                   // Amber
  '#ec4899',                   // Pink
  '#06b6d4',                   // Cyan
];

const getColor = (index) => colors[index % colors.length];

const chartData = computed(() => {
  if (!performanceData.value?.gamers) return [];
  return performanceData.value.gamers.map(g => ({
    ...g,
    shortName: g.gamerName,
  }));
});

const hasData = computed(() => chartData.value.length > 0 && chartData.value.some(g => g.dataPoints?.length > 0));

// Y-axis grid lines for winrate (0-100%)
const yGridLines = computed(() => {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return [0, 25, 50, 75, 100].map(pct => padding.top + plotHeight * (1 - pct / 100));
});

const yLabelsWinrate = [0, 25, 50, 75, 100];

// Y-axis for economy (gold/cs per min) - dynamic based on data
const maxEconValue = computed(() => {
  let max = 10;
  chartData.value.forEach(g => {
    g.dataPoints?.forEach(d => {
      max = Math.max(max, d.goldPerMin / 30, d.csPerMin); // Scale gold down for comparison
    });
  });
  return Math.ceil(max / 2) * 2; // Round up to even number
});

const yGridLinesEcon = computed(() => {
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return [0, 0.25, 0.5, 0.75, 1].map(frac => padding.top + plotHeight * frac);
});

const yLabelsGold = computed(() => {
  const max = maxEconValue.value;
  return [max, max * 0.75, max * 0.5, max * 0.25, 0].map(v => Math.round(v));
});

// Convert data points to SVG polyline points
function getWinratePoints(gamer) {
  const points = gamer.dataPoints || [];
  if (points.length === 0) return '';
  
  const plotWidth = chartWidth - padding.left - padding.right;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const maxGames = Math.max(...chartData.value.map(g => g.dataPoints?.length || 0));
  
  return points.map((d, i) => {
    const x = padding.left + (i / Math.max(1, maxGames - 1)) * plotWidth;
    const y = padding.top + plotHeight * (1 - d.winrate / 100);
    return `${x},${y}`;
  }).join(' ');
}

function getGoldPoints(gamer) {
  const points = gamer.dataPoints || [];
  if (points.length === 0) return '';
  
  const plotWidth = chartWidth - padding.left - padding.right;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const maxGames = Math.max(...chartData.value.map(g => g.dataPoints?.length || 0));
  const max = maxEconValue.value * 30; // Scale gold back up
  
  return points.map((d, i) => {
    const x = padding.left + (i / Math.max(1, maxGames - 1)) * plotWidth;
    const y = padding.top + plotHeight * (1 - Math.min(d.goldPerMin, max) / max);
    return `${x},${y}`;
  }).join(' ');
}

function getCsPoints(gamer) {
  const points = gamer.dataPoints || [];
  if (points.length === 0) return '';
  
  const plotWidth = chartWidth - padding.left - padding.right;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const maxGames = Math.max(...chartData.value.map(g => g.dataPoints?.length || 0));
  const max = maxEconValue.value;
  
  return points.map((d, i) => {
    const x = padding.left + (i / Math.max(1, maxGames - 1)) * plotWidth;
    const y = padding.top + plotHeight * (1 - Math.min(d.csPerMin, max) / max);
    return `${x},${y}`;
  }).join(' ');
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    performanceData.value = await getPerformance(props.userId, limit.value);
  } catch (e) {
    error.value = e?.message || 'Failed to load performance data.';
    performanceData.value = null;
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch(() => props.userId, load);
watch(limit, load);
</script>

<style scoped>
.performance-charts {
  margin-top: 2rem;
  padding: 1rem;
  background: var(--color-bg-elev);
  border-radius: 12px;
  border: 1px solid var(--color-border);
}

.charts-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 1rem;
}

.charts-header h3 {
  margin: 0;
  font-size: 1.1rem;
  color: var(--color-text);
}

.limit-toggle {
  display: flex;
  gap: 0.25rem;
}

.limit-btn {
  padding: 0.35rem 0.75rem;
  font-size: 0.85rem;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  color: var(--color-text-muted);
  cursor: pointer;
  transition: all 0.15s ease;
}

.limit-btn:first-child { border-radius: 6px 0 0 6px; }
.limit-btn:last-child { border-radius: 0 6px 6px 0; }

.limit-btn.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: var(--color-text);
}

.limit-btn:hover:not(.active) {
  background: var(--color-bg-hover);
}

.charts-loading,
.charts-error,
.charts-empty {
  text-align: center;
  padding: 2rem;
  color: var(--color-text-muted);
}

.charts-error {
  color: var(--color-danger);
}

.charts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 1.5rem;
}

.chart-card {
  background: var(--color-bg);
  border-radius: 8px;
  padding: 1rem;
  border: 1px solid var(--color-border);
}

.chart-card h4 {
  margin: 0 0 0.75rem 0;
  font-size: 0.95rem;
  font-weight: 500;
  color: var(--color-text-muted);
}

.line-chart {
  width: 100%;
  height: auto;
  max-height: 200px;
}

.line-chart .grid line {
  stroke: var(--color-border);
  stroke-width: 1;
}

.line-chart .y-labels text {
  font-size: 10px;
  fill: var(--color-text-muted);
}

.line-chart .legend-text {
  font-size: 10px;
  fill: var(--color-text-muted);
}

.chart-footnote {
  font-size: 0.75rem;
  color: var(--color-text-muted);
  text-align: center;
  margin-top: 0.5rem;
  opacity: 0.8;
}

/* Mobile stacking */
@media (max-width: 768px) {
  .charts-header {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .charts-grid {
    grid-template-columns: 1fr;
  }
}
</style>
