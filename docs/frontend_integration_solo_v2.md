# Frontend Integration Guide - Solo Dashboard v2

**Endpoint:** `GET /api/v2/solo/dashboard/{userId}`

---

## Usage in Vue 3

### 1. Add API Function

Update `/client/src/api/solo.js`:

```javascript
import axios from 'axios';
import { getBaseApi } from './shared.js';

// ========================
// Solo Dashboard V2 (New)
// ========================

export async function getSoloDashboardV2(userId, queueType = 'all') {
  const base = getBaseApi();
  const params = { queueType };
  const { data } = await axios.get(`${base}/solo/dashboard/${userId}`, { params });
  return data;
}

// Keep existing v1 functions for backwards compatibility
// ...
```

### 2. Use in Vue Component

```vue
<template>
  <div class="solo-dashboard">
    <div v-if="loading" class="spinner">Loading...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else class="dashboard-content">
      <!-- Overall Stats -->
      <StatsCard
        :games="dashboard.gamesPlayed"
        :wins="dashboard.wins"
        :winRate="dashboard.winRate"
        :kda="dashboard.avgKda"
        :duration="dashboard.avgGameDurationMinutes"
      />

      <!-- Side Statistics -->
      <SideWinDistribution
        :blueWins="dashboard.sideStats.blueWins"
        :redWins="dashboard.sideStats.redWins"
        :blueDistribution="dashboard.sideStats.blueWinDistribution"
        :redDistribution="dashboard.sideStats.redWinDistribution"
      />

      <!-- Main Champion -->
      <ChampionCard
        v-if="dashboard.mainChampion"
        :champion="dashboard.mainChampion"
      />

      <!-- Performance by Phase -->
      <PerformanceChart
        :phases="dashboard.performanceByPhase"
      />

      <!-- Role Breakdown -->
      <RoleStats
        :roles="dashboard.roleBreakdown"
      />

      <!-- Death Efficiency -->
      <DeathAnalysis
        :deaths="dashboard.deathEfficiency"
      />

      <!-- Trends -->
      <TrendComparison
        :last10="dashboard.last10Games"
        :last20="dashboard.last20Games"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { getSoloDashboardV2 } from '@/api/solo.js';

const props = defineProps({
  userId: { type: [String, Number], required: true }
});

const dashboard = ref(null);
const loading = ref(true);
const error = ref(null);

const fetchDashboard = async (queueType = 'all') => {
  try {
    loading.value = true;
    dashboard.value = await getSoloDashboardV2(props.userId, queueType);
    error.value = null;
  } catch (err) {
    error.value = err.message || 'Failed to load dashboard';
    dashboard.value = null;
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  fetchDashboard();
});

// Allow queue filter switching
const onQueueFilterChange = (queueType) => {
  fetchDashboard(queueType);
};

defineExpose({
  fetchDashboard,
  onQueueFilterChange
});
</script>

<style scoped>
.solo-dashboard {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  padding: 20px;
}

.dashboard-content {
  display: contents;
}

.spinner {
  text-align: center;
  padding: 40px;
}

.error {
  color: red;
  padding: 20px;
  background: #ffe0e0;
  border-radius: 8px;
}

@media (max-width: 768px) {
  .solo-dashboard {
    grid-template-columns: 1fr;
  }
}
</style>
```

### 3. Update Router (if needed)

Update `/client/src/router/index.js` to use the new v2 endpoint:

```javascript
{
  path: '/solo/dashboard/:userId',
  name: 'SoloDashboardV2',
  component: () => import('@/views/SoloDashboard.vue'),
  props: true
}
```

### 4. Queue Filter Component

Create `/client/src/components/shared/QueueFilter.vue`:

```vue
<template>
  <div class="queue-filter">
    <label>Queue Type:</label>
    <select v-model="selectedQueue" @change="onQueueChange">
      <option value="all">All Queues</option>
      <option value="ranked_solo">Ranked Solo/Duo</option>
      <option value="ranked_flex">Ranked Flex</option>
      <option value="normal">Normal</option>
      <option value="aram">ARAM</option>
    </select>
  </div>
</template>

<script setup>
import { ref, emit } from 'vue';

const selectedQueue = ref('all');

const onQueueChange = () => {
  emit('queue-changed', selectedQueue.value);
};
</script>

<style scoped>
.queue-filter {
  display: flex;
  align-items: center;
  gap: 10px;
  margin: 15px 0;
}

select {
  padding: 8px 12px;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-size: 14px;
}
</style>
```

### 5. Usage in Dashboard View

Update `/client/src/views/SoloDashboard.vue`:

```vue
<template>
  <div class="solo-dashboard-container">
    <h1>Solo Performance Dashboard</h1>
    
    <QueueFilter @queue-changed="onQueueFilterChange" />
    
    <SoloDashboard
      ref="dashboardRef"
      :userId="userId"
    />
  </div>
</template>

<script setup>
import { ref } from 'vue';
import SoloDashboard from '@/components/SoloDashboard.vue';
import QueueFilter from '@/components/shared/QueueFilter.vue';

const props = defineProps({
  userId: { type: [String, Number], required: true }
});

const dashboardRef = ref(null);

const onQueueFilterChange = (queueType) => {
  dashboardRef.value?.onQueueFilterChange(queueType);
};
</script>

<style scoped>
.solo-dashboard-container {
  padding: 20px;
}

h1 {
  margin-bottom: 20px;
}
</style>
```

---

## Response Shape to Component Mapping

| API Field | Component | Purpose |
|-----------|-----------|---------|
| `gamesPlayed`, `wins`, `winRate` | StatsCard | Overview statistics |
| `avgKda`, `avgGameDurationMinutes` | StatsCard | Performance metrics |
| `sideStats.*` | SideWinDistribution | Win breakdown by side |
| `mainChampion` | ChampionCard | Most-played or best champion |
| `performanceByPhase` | PerformanceChart | Early/Mid/Late game trends |
| `roleBreakdown` | RoleStats | Performance by position |
| `deathEfficiency` | DeathAnalysis | Death timing patterns |
| `last10Games`, `last20Games` | TrendComparison | Recent momentum |

---

## Error Handling

```javascript
try {
  const dashboard = await getSoloDashboardV2(userId, queueType);
  // Render dashboard
} catch (error) {
  if (error.response?.status === 400) {
    console.error('Invalid userId format');
  } else if (error.response?.status === 404) {
    console.error('No match data found for this player');
  } else {
    console.error('Server error:', error.message);
  }
}
```

---

## Caching Strategy (Optional)

```javascript
// Simple in-memory cache
const dashboardCache = new Map();

export async function getSoloDashboardV2Cached(userId, queueType = 'all') {
  const cacheKey = `${userId}:${queueType}`;
  
  if (dashboardCache.has(cacheKey)) {
    return dashboardCache.get(cacheKey);
  }
  
  const data = await getSoloDashboardV2(userId, queueType);
  dashboardCache.set(cacheKey, data);
  
  // Invalidate cache after 5 minutes
  setTimeout(() => dashboardCache.delete(cacheKey), 5 * 60 * 1000);
  
  return data;
}
```

---

## Backwards Compatibility

The new v2 endpoint coexists with v1. To migrate:

1. **Phase 1:** Deploy v2 endpoint alongside existing code
2. **Phase 2:** Update components to use getSoloDashboardV2()
3. **Phase 3:** Test thoroughly with real data
4. **Phase 4:** Deprecate v1 endpoint after 6 months
5. **Phase 5:** Remove v1 endpoint in next major version

This allows gradual rollout without breaking existing clients.
