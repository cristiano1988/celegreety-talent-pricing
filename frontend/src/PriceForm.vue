<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import PriceInput from './PriceInput.vue'

interface Props {
    personalPrice: number | null
    businessPrice: number | null
    loading?: boolean
    showChangeReason?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    loading: false,
    showChangeReason: false
})

const emit = defineEmits<{
    (e: 'submit', payload: {
        personalPrice: number
        businessPrice: number
        changeReason?: string
    }): void
}>()

const personal = ref<number | null>(props.personalPrice)
const business = ref<number | null>(props.businessPrice)
const changeReason = ref('')

watch(() => props.personalPrice, (v: number | null) => personal.value = v)
watch(() => props.businessPrice, (v: number | null) => business.value = v)

const isValid = computed(() => {
    if (personal.value == null || business.value == null) return false
    const MAX_PRICE = 99999999 // 999,999.99 EUR
    return business.value >= personal.value && 
           personal.value <= MAX_PRICE && 
           business.value <= MAX_PRICE
})

function onSubmit() {
if (!isValid.value) return

emit('submit', {
personalPrice: personal.value!,
businessPrice: business.value!,
changeReason: props.showChangeReason ? changeReason.value : undefined
})
}
</script>
<template>
    <form class="space-y-4" @submit.prevent="onSubmit">
        <PriceInput
            v-model="personal"
            label="Personal price"
            type="personal"
        />

        <PriceInput
            v-model="business"
            label="Business price"
            type="business"
        />

        <div v-if="personal && business && business < personal" class="alert alert-error">
        Business price must be greater or equal to personal price
        </div>

        <div v-if="(personal && personal > 99999999) || (business && business > 99999999)" class="alert alert-error">
        Price cannot exceed €999,999.99 (Stripe limit)
        </div>

        <textarea
            v-if="showChangeReason"
            v-model="changeReason"
            class="textarea textarea-bordered w-full"
            placeholder="Change reason (optional)"
        />

        <button
            type="submit"
            class="btn btn-primary w-full"
            :disabled="!isValid || loading"
        >
            <span v-if="loading" class="loading loading-spinner"></span>
            <span v-else>Save pricing</span>
        </button>
    </form>
</template>