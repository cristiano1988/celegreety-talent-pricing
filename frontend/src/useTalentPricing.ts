import { ref, computed, onMounted, onUnmounted } from 'vue'
import axios, { AxiosError } from 'axios'

export interface Pricing {
    talentId: number
    personalPrice: number
    businessPrice: number
    stripeProductId: string
    stripePersonalPriceId: string
    stripeBusinessPriceId: string
    pricesLastSyncedAt: string
    version: number
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

        if (!pricing.value) return

        try {
            await axios.put('/api/talentpricing', {
                talentId,
                personalPrice: payload.personalPrice,
                businessPrice: payload.businessPrice,
                changeReason: payload.changeReason,
                version: pricing.value.version
            })
            await fetchPricing()
        } catch (e: unknown) {
            const err = e as AxiosError
            // Ideally backend returns 409, but currently it throws 500 with message. 
            // We should check response data. assuming 500 for now based on 'throw Exception' in SQL.
            // If backend mapped specific exception to 409, we'd check that.
            if (err.response?.status === 500  /* || err.response?.status === 409 */) {
                // Simple error message for now, exact matching depends on backend error serialization
                error.value = 'Pricing was updated by another user. Please refresh.'
            } else {
                error.value = 'Failed to update pricing'
            }
        } finally {
            loading.value = false
        }
    }

    const handleFocus = () => {
        fetchPricing()
    }

    onMounted(() => {
        window.addEventListener('focus', handleFocus)
    })

    onUnmounted(() => {
        window.removeEventListener('focus', handleFocus)
    })

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