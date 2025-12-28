<template>
  <div class="radar-chart-container">
    <div v-if="loading" class="radar-loading">Loading radar data…</div>
    <div v-else-if="error" class="radar-error">{{ error }}</div>
    <div v-else-if="!hasData" class="radar-empty">No radar data available.</div>
    
    <ChartCard v-else title="Radar Chart – EUW vs EUNE">
      <svg :viewBox="`0 0 ${size} ${size}`" class="radar-svg" aria-label="Performance radar chart">
        <!-- Background grid circles -->
        <g class="grid-circles">
          <circle v-for="level in [0.2, 0.4, 0.6, 0.8, 1]" :key="'grid-' + level"
            :cx="centerX" :cy="centerY" :r="radius * level"
            fill="none" stroke="var(--color-border)" stroke-width="1" opacity="0.3" />
        </g>
        
        <!-- Axis lines and labels -->
        <g class="axes">
          <g v-for="(metric, i) in metrics" :key="'axis-' + i">
            <line 
              :x1="centerX" :y1="centerY"
              :x2="getAxisPoint(i).x" :y2="getAxisPoint(i).y"
              stroke="var(--color-border)" stroke-width="1" opacity="0.5" />
            <text 
              :x="getLabelPoint(i).x" :y="getLabelPoint(i).y"
              text-anchor="middle" dominant-baseline="middle"
              class="axis-label">
              {{ metric.label }}
            </text>
          </g>
        </g>
        
        <!-- Data polygons for each gamer -->
        <g class="data-polygons">
          <g v-for="(gamer, gi) in radarData" :key="'polygon-' + gi">
            <polygon
              :points="getPolygonPoints(gamer)"
              :fill="getColor(gi)"
              :stroke="getColor(gi)"
              fill-opacity="0.2"
              stroke-width="2"
              stroke-linejoin="round" />
            <!-- Data points -->
            <circle v-for="(value, mi) in gamer.values" :key="'point-' + gi + '-' + mi"
              :cx="getDataPoint(mi, value).x" :cy="getDataPoint(mi, value).y"
              :fill="getColor(gi)" r="4" />
          </g>
        </g>
        
        <!-- Legend -->
        <g class="legend" :transform="`translate(${legendX}, ${legendY})`">
          <g v-for="(gamer, gi) in radarData" :key="'legend-' + gi"
            :transform="`translate(0, ${gi * 20})`">
            <rect :fill="getColor(gi)" width="14" height="14" rx="2" />
            <text x="20" y="11" class="legend-text">{{ gamer.name }}</text>
          </g>
        </g>
      </svg>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getComparison from '@/assets/getComparison.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const comparisonData = ref(null);

// Chart dimensions
const size = 500;
const centerX = size / 2;
const centerY = size / 2;
const radius = 160;
const labelOffset = 50; // Increased from 30 to move labels further from graph
const legendX = 20;
const legendY = size - 60;

// Metrics configuration with normalization ranges
const metrics = [
  { key: 'kills', label: 'Kills', max: 10 },
  { key: 'deaths', label: 'Deaths', max: 10, inverse: true }, // Lower is better
  { key: 'assists', label: 'Assists', max: 10 },
  { key: 'csPerMin', label: 'CS/min', max: 10 },
  { key: 'goldPerMin', label: 'Gold/min', max: 600 },
  { key: 'timeDead', label: 'Time Dead', max: 300, inverse: true }, // Lower is better
];

const colors = [
  'var(--color-primary)',      // Purple
  'var(--color-success)',      // Green
  '#f59e0b',                   // Amber
  '#ec4899',                   // Pink
];

const getColor = (index) => colors[index % colors.length];

// Get axis endpoint coordinates
function getAxisPoint(index) {
  const angle = (Math.PI * 2 * index) / metrics.length - Math.PI / 2;
  return {
    x: centerX + radius * Math.cos(angle),
    y: centerY + radius * Math.sin(angle),
  };
}

// Get label position (further out from axis endpoint)
function getLabelPoint(index) {
  const angle = (Math.PI * 2 * index) / metrics.length - Math.PI / 2;
  return {
    x: centerX + (radius + labelOffset) * Math.cos(angle),
    y: centerY + (radius + labelOffset) * Math.sin(angle),
  };
}

// Get data point coordinates
function getDataPoint(metricIndex, normalizedValue) {
  const angle = (Math.PI * 2 * metricIndex) / metrics.length - Math.PI / 2;
  const r = radius * normalizedValue;
  return {
    x: centerX + r * Math.cos(angle),
    y: centerY + r * Math.sin(angle),
  };
}

// Generate polygon points string
function getPolygonPoints(gamer) {
  return gamer.values
    .map((value, i) => {
      const point = getDataPoint(i, value);
      return `${point.x},${point.y}`;
    })
    .join(' ');
}

// Compute radar data from comparison data
const radarData = computed(() => {
  if (!comparisonData.value) return [];

  // Extract gamer names from any metric array
  const gamerNames = comparisonData.value.winrate?.map(g => g.gamerName) || [];

  return gamerNames.map(name => {
    // Get actual data from the API
    const kills = comparisonData.value.avgKills?.find(g => g.gamerName === name)?.value || 0;
    const deaths = comparisonData.value.avgDeaths?.find(g => g.gamerName === name)?.value || 0;
    const assists = comparisonData.value.avgAssists?.find(g => g.gamerName === name)?.value || 0;
    const csPerMin = comparisonData.value.csPrMin?.find(g => g.gamerName === name)?.value || 0;
    const goldPerMin = comparisonData.value.goldPrMin?.find(g => g.gamerName === name)?.value || 0;
    const timeDead = comparisonData.value.avgTimeDeadSeconds?.find(g => g.gamerName === name)?.value || 0;

    // Normalize values to 0-1 range
    const values = metrics.map(metric => {
      let rawValue;
      switch (metric.key) {
        case 'kills': rawValue = kills; break;
        case 'deaths': rawValue = deaths; break;
        case 'assists': rawValue = assists; break;
        case 'csPerMin': rawValue = csPerMin; break;
        case 'goldPerMin': rawValue = goldPerMin; break;
        case 'timeDead': rawValue = timeDead; break;
        default: rawValue = 0;
      }

      // Normalize to 0-1
      let normalized = Math.min(rawValue / metric.max, 1);

      // For inverse metrics (lower is better), invert the normalized value
      if (metric.inverse) {
        normalized = 1 - normalized;
      }

      return Math.max(0, normalized);
    });

    return {
      name,
      values,
    };
  });
});

const hasData = computed(() => radarData.value.length > 0);

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    comparisonData.value = await getComparison(props.userId);
  } catch (e) {
    console.error('Error loading radar chart data:', e);
    error.value = e?.message || 'Failed to load radar chart data.';
    comparisonData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.radar-chart-container {
  width: 100%;
  max-width: 100%;
}

/* Override ChartCard max-width for radar chart */
.radar-chart-container :deep(.chart-card) {
  max-width: 100%;
  height: 350px;
}

.radar-loading,
.radar-error,
.radar-empty {
  text-align: center;
  padding: 2rem;
  color: var(--color-text-muted);
}

.radar-error {
  color: var(--color-danger);
}

.radar-svg {
  width: 100%;
  height: 100%;
  display: block;
}

.axis-label {
  font-size: 13px;
  font-weight: 600;
  fill: var(--color-text);
}

.legend-text {
  font-size: 13px;
  font-weight: 600;
  fill: var(--color-text);
}
</style>

