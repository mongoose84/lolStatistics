<template>
  <section class="userview">
    <h2 v-if="hasUser">User: {{ userName }} (ID: {{ userId }})</h2>
    <h2 v-else>Missing user details</h2>

    <div v-if="!hasUser">Please navigate via the Users list.</div>
    <div v-else-if="loading">Loading…</div>
    <div v-else-if="error">{{ error }}</div>

    <!-- Pretty‑print the returned object -->
    <pre v-else-if="summoner">
  {{ JSON.stringify(summoner, null, 2) }}
  <br />
  Win‑rate: {{ winRate !== null ? `${winRate}%` : '—' }}
</pre>
  </section>
</template>

<script setup>
import { onMounted, watch, computed, ref } from 'vue';

// ----- Props coming from the parent (router, other component, etc.) -----
const props = defineProps({
  userName: {
    type: String,
    required: true,
  },
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const summoner = ref(null);
const winRate = ref(null);

const hasUser = computed(() => !!props.userName && props.userId !== undefined && props.userId !== '' );

function load() {
  if (!hasUser.value) return;
  // Placeholder for future fetches based on userId/userName
}

onMounted(() => {
  load();
});

watch(
  () => [props.userName, props.userId],
  () => {
    load();
  }
);

// (Optional) expose `load` so a parent could call it manually
defineExpose({ load });
</script>

<style scoped>
.userview {
  max-width: 800px;
  margin: 4rem auto;
}
</style>