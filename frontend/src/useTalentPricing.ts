import { ref, computed } from 'vue'
import axios, { AxiosError } from 'axios'

export interface Pricing {
    talentId: number
    personalPrice: number
    businessPrice: number
    stripeProductId: string
    stripePersonalPriceId: string
    stripeBusinessPriceId: string
    pricesLastSyncedAt: string
}

export function useTalentPricing(talentId: number) {
    const pricing = ref<Pricing | null>(null)
    const loading = ref(false)
    const error = ref<string | null>(null)

    const hasExistingPricing = computed(() => !!pricing.value)

    async function fetchPricing() {
        loading.value = true
        error.value = null

        try {
            const { data } = await axios.get<Pricing>(`/api/talentpricing/${talentId}`)
            pricing.value = data
        } catch (e: unknown) {
            const err = e as AxiosError
            if (err.response?.status !== 404)
                error.value = 'Failed to load pricing'
        } finally {
            loading.value = false
        }
    }

    async function createPricing(payload: {
        personalPrice: number
        businessPrice: number
    }) {
        loading.value = true
        error.value = null

        try {
            await axios.post('/api/talentpricing', {
                talentId,
                personalPrice: payload.personalPrice,
                businessPrice: payload.businessPrice,
                currency: 'EUR'
            })
            await fetchPricing()
        } catch (e: unknown) {
            error.value = 'Failed to create pricing'
        } finally {
            loading.value = false
        }
    }

    async function updatePricing(payload: {
        personalPrice: number
        businessPrice: number
        changeReason?: string
    }) {
        loading.value = true
        error.value = null

        try {
            await axios.put('/api/talentpricing', {
                talentId,
                personalPrice: payload.personalPrice,
                businessPrice: payload.businessPrice,
                changeReason: payload.changeReason
            })
            await fetchPricing()
        } catch (e: unknown) {
            error.value = 'Failed to update pricing'
        } finally {
            loading.value = false
        }
    }

    return {
        pricing,
        loading,
        error,
        hasExistingPricing,
        fetchPricing,
        createPricing,
        updatePricing
    }
}