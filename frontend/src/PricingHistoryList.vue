<script setup lang="ts">
import { PricingHistory } from './useTalentPricing'

defineProps<{
    history: PricingHistory[]
}>()

function formatDate(dateString: string) {
    return new Date(dateString).toLocaleString()
}

function formatCurrency(cents: number) {
    return new Intl.NumberFormat('de-DE', {
        style: 'currency',
        currency: 'EUR'
    }).format(cents / 100)
}
</script>

<template>
    <div class="card bg-base-100 shadow mt-6">
        <div class="card-body">
            <h2 class="card-title text-lg mb-4">Pricing History</h2>
            
            <div v-if="history.length === 0" class="text-gray-500 italic">
                No history available yet.
            </div>
            
            <div v-else class="overflow-x-auto">
                <table class="table table-zebra w-full text-sm">
                    <thead>
                        <tr>
                            <th>Date</th>
                            <th>Personal</th>
                            <th>Business</th>
                            <th>Reason</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in history" :key="item.createdAt">
                            <td class="whitespace-nowrap">{{ formatDate(item.createdAt) }}</td>
                            <td>{{ formatCurrency(item.personalPrice) }}</td>
                            <td>{{ formatCurrency(item.businessPrice) }}</td>
                            <td class="max-w-xs truncate" :title="item.changeReason">
                                {{ item.changeReason || '-' }}
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</template>
