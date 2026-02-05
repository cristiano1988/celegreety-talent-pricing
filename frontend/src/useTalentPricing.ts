import { ref, computed, onMounted, onUnmounted } from 'vue'
import { AxiosError } from 'axios'
import api from './api'

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

export interface PricingHistory {
    personalPrice: number
    businessPrice: number
    changeReason: string
    createdAt: string
}

export interface PricingWithHistory {
    current: Pricing
    history: PricingHistory[]
}

export function useTalentPricing(talentId: number) {
    const pricing = ref<Pricing | null>(null)
    const history = ref<PricingHistory[]>([])
    const loading = ref(false)
    const error = ref<string | null>(null)

    const hasExistingPricing = computed(() => !!pricing.value)

    async function fetchPricing() {
        loading.value = true
        error.value = null

        try {
            const { data } = await api.get<PricingWithHistory>(`/talentpricing/${talentId}`)
            pricing.value = data.current
            history.value = data.history
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
            await api.post('/talentpricing', {
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
            await api.put('/talentpricing', {
                talentId,
                personalPrice: payload.personalPrice,
                businessPrice: payload.businessPrice,
                changeReason: payload.changeReason,
                version: pricing.value.version
            })
            await fetchPricing()
        } catch (e: unknown) {
            const err = e as AxiosError
            const data = err.response?.data as { error?: string } | undefined

            if (err.response?.status === 409) {
                error.value = 'Pricing was updated by another user. Please refresh.'
            } else if (err.response?.status === 400 && data?.error) {
                error.value = data.error
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
        history,
        loading,
        error,
        hasExistingPricing,
        fetchPricing,
        createPricing,
        updatePricing
    }
}