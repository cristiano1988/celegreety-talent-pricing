<script setup lang="ts">
import TalentPricingManagement from './TalentPricingManagement.vue'

// In a real app, this ID would come from a route parameter or auth context
const DEMO_TALENT_ID = 123
</script>

<template>
  <div class="min-h-screen bg-base-200 py-10 px-4">
    <TalentPricingManagement :talentId="DEMO_TALENT_ID" />
  </div>
</template>
