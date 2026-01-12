<template>
  <div class="queue-filter">
    <div class="filter-buttons">
      <button
        v-for="option in queueOptions"
        :key="option.value"
        class="filter-btn"
        :class="{ active: modelValue === option.value }"
        @click="$emit('update:modelValue', option.value)"
      >
        {{ option.label }}
      </button>
    </div>
  </div>
</template>

<script setup>
defineProps({
  modelValue: {
    type: String,
    default: 'all_ranked'
  }
})

defineEmits(['update:modelValue'])

const queueOptions = [
  { value: 'all_ranked', label: 'All Ranked' },
  { value: 'ranked_solo', label: 'Solo/Duo' },
  { value: 'ranked_flex', label: 'Flex' },
  { value: 'normal', label: 'Normal' },
  { value: 'aram', label: 'ARAM' }
]
</script>

<style scoped>
.queue-filter {
  display: flex;
  align-items: center;
}

.filter-buttons {
  display: flex;
  gap: 2px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 2px;
}

.filter-btn {
  padding: var(--spacing-xs) var(--spacing-md);
  background: transparent;
  border: none;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  border-radius: var(--radius-sm);
  transition: all 0.2s;
  white-space: nowrap;
}

.filter-btn:hover {
  color: var(--color-text);
  background: var(--color-surface-hover);
}

.filter-btn.active {
  background: var(--color-primary);
  color: white;
}

@media (max-width: 600px) {
  .filter-buttons {
    flex-wrap: wrap;
  }
  
  .filter-btn {
    padding: var(--spacing-xs) var(--spacing-sm);
    font-size: var(--font-size-xs);
  }
}
</style>

