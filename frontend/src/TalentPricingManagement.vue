<script setup lang="ts">
import { onMounted } from 'vue'
import PricingForm from './PriceForm.vue'
import { useTalentPricing } from './useTalentPricing'

const props = defineProps<{
    talentId: number
}>()

const {
    pricing,
    history,
    loading,
    error,
    hasExistingPricing,
    fetchPricing,
    createPricing,
    updatePricing
} = useTalentPricing(props.talentId)

import PricingHistoryList from './PricingHistoryList.vue'

onMounted(fetchPricing)

async function handleSubmit(payload: {
    personalPrice: number
    businessPrice: number
    changeReason?: string
}) {
    if (hasExistingPricing.value) {
        await updatePricing(payload)
    } else {
        await createPricing(payload)
    }
}
</script>
<template>
    <div class="max-w-xl mx-auto space-y-6">
        <h1 class="text-2xl font-bold">Talent pricing</h1>

        <div v-if="error" class="alert alert-error">
            {{ error }}
        </div>

        <div v-if="pricing" class="alert alert-info">
            <p>Current pricing:</p>
            <ul class="list-disc ml-5">
                <li>Personal: €{{ (pricing.personalPrice / 100).toFixed(2) }}</li>
                <li>Business: €{{ (pricing.businessPrice / 100).toFixed(2) }}</li>
            </ul>
        </div>

        <transition name="fade">
            <PricingForm
                :personalPrice="pricing?.personalPrice ?? null"
                :businessPrice="pricing?.businessPrice ?? null"
                :loading="loading"
                :showChangeReason="hasExistingPricing"
                @submit="handleSubmit"
            />
            <div v-if="pricing?.pricesLastSyncedAt" class="text-xs text-gray-500 mt-2 text-center">
                Last synced: {{ new Date(pricing.pricesLastSyncedAt).toLocaleString() }}
            </div>
        </transition>

        <transition name="slide-up">
            <PricingHistoryList v-if="history.length > 0" :history="history" />
        </transition>
    </div>
</template>

<style scoped>
.fade-enter-active, .fade-leave-active {
    transition: opacity 0.3s ease;
}
.fade-enter-from, .fade-leave-to {
    opacity: 0;
}

.slide-up-enter-active, .slide-up-leave-active {
    transition: all 0.4s ease-out;
}
.slide-up-enter-from {
    transform: translateY(20px);
    opacity: 0;
}
</style>