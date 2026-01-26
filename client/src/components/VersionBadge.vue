<template>
  <div
    class="fixed left-4 bottom-4 py-1.5 px-2.5 border border-border rounded-md bg-[rgba(0,0,0,0.6)] backdrop-blur-[8px] text-text-secondary text-xs tracking-tight z-[1000]"
    aria-label="App Version"
  >
    <span class="text-text font-medium">Mongoose.gg <span class="text-[0.5em] text-text-secondary font-normal align-top">Beta</span></span>
    <span class="mx-1.5 opacity-70">•</span>
    <span class="text-primary font-semibold">v{{ version }}</span>
  </div>
</template>

<script setup>
import pkg from '../../package.json'

const version = pkg.version || '0.0.0'
</script>
