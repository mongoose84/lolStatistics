<!-- src/App.vue -->
<template>
  <!-- Optional global layout -->
  <div id="app" data-testid="app-root">
    

    <!-- This is where the matched route component (Home or Results) is rendered -->
    <router-view />
  </div>
</template>

<script setup>
// No script logic needed for the root component in this simple case
</script>