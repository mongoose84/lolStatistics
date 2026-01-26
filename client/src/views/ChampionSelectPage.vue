<template>
  <section class="p-lg" data-testid="champion-select-page">
    <h1 class="sr-only">Champion Select</h1>

    <!-- Header with centered Queue Toggle -->
    <header class="flex items-center justify-center mb-lg" data-testid="champion-select-header">
      <!-- Queue Toggle Bar (centered) -->
      <div class="flex border border-border rounded-md overflow-hidden bg-background-surface" role="group" aria-label="Filter by queue type">
        <button
          v-for="queue in queueOptions"
          :key="queue.value"
          type="button"
          class="queue-toggle-btn py-sm px-md bg-transparent border-none text-text-secondary text-sm font-medium cursor-pointer transition-colors duration-200 relative focus:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary-soft"
          :class="{
            'queue-toggle-btn--active': queueFilter === queue.value,
            'hover:text-text hover:bg-background-elevated': queueFilter !== queue.value
          }"
          @click="queueFilter = queue.value"
          :aria-pressed="queueFilter === queue.value"
        >
          {{ queue.label }}
        </button>
      </div>
    </header>

    <!-- Main content area -->
    <div class="flex flex-col gap-lg">
      <div class="bg-background-surface border border-border rounded-lg p-2xl text-center">
        <p class="text-lg text-text-secondary">
          Champion Select components coming soon...
        </p>
        <p class="text-sm text-text-secondary mt-sm">
          Current filter: <span class="text-primary font-medium">{{ currentQueueLabel }}</span>
        </p>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, computed } from 'vue'

// UI state for queue filter
const queueFilter = ref('ranked_solo')

// Queue options for toggle bar
const queueOptions = [
  { value: 'all', label: 'All Queues' },
  { value: 'ranked_solo', label: 'Ranked Solo/Duo' },
  { value: 'ranked_flex', label: 'Ranked Flex' },
  { value: 'normal', label: 'Normal' },
  { value: 'aram', label: 'ARAM' }
]

// Computed label for current queue
const currentQueueLabel = computed(() => {
  const option = queueOptions.find(q => q.value === queueFilter.value)
  return option ? option.label : 'All Queues'
})
</script>

<style scoped>
/* Active state with darker purple for better visibility */
.queue-toggle-btn--active {
  background-color: #5b21b6; /* Darker purple (violet-800) */
  color: white;
}

/* Queue toggle button dividers (pseudo-elements can't be done in Tailwind) */
.queue-toggle-btn:not(:last-child)::after {
  content: '';
  position: absolute;
  right: 0;
  top: 25%;
  height: 50%;
  width: 1px;
  background: var(--color-border);
}

/* Hide divider when button is active or next to active */
.queue-toggle-btn--active::after {
  display: none;
}

.queue-toggle-btn:has(+ .queue-toggle-btn--active)::after {
  display: none;
}
</style>
