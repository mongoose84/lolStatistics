<template>
  <div class="app-container">
    <RouterView />
  </div>
</template>

<script setup>
import { RouterView } from 'vue-router'
</script>

<style scoped>
.app-container {
  min-height: 100vh;
}
</style>
