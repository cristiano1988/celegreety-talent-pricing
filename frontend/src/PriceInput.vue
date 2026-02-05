<script setup lang="ts">
import { computed } from 'vue'
import currency from 'currency.js'

interface Props {
    modelValue: number | null
    label: string
    type: 'personal' | 'business'
}

const props = defineProps<Props>()

const emit = defineEmits<{
    (e: 'update:modelValue', value: number | null): void
}>()



const displayValue = computed({
    get() {
        if (props.modelValue == null) return ''
        return currency(props.modelValue, { fromCents: true }).toString()
    },
    set(value: string) {
        // Only allow digits and at most one dot/comma, with max 2 decimals
        const cleaned = value.replace(',', '.')
        if (cleaned !== '' && !/^\d+(\.\d{0,2})?$/.test(cleaned)) {
            return
        }

        const parsed = Number(cleaned)

        if (isNaN(parsed) || parsed <= 0) {
            emit('update:modelValue', null)
            return
        }

        emit('update:modelValue', currency(cleaned).intValue)
    }
})

const onKeyDown = (event: KeyboardEvent) => {
    // Prevent non-numeric chars except functional keys and decimal separator
    const isControlKey = event.ctrlKey || event.metaKey || event.altKey;
    const isAllowedModifier = ['Backspace', 'Delete', 'ArrowLeft', 'ArrowRight', 'Tab', 'Enter'].includes(event.key);
    const isNumber = /^\d$/.test(event.key);
    const isSeparator = event.key === '.' || event.key === ',';

    if (!isNumber && !isSeparator && !isAllowedModifier && !isControlKey) {
        event.preventDefault();
    }
};
</script>

<template>
    <div class="form-control w-full">
        <label class="label">
            <span class="label-text">{{ label }}</span>
        </label>

        <div class="input-group">
            <span class="bg-base-200 px-3 flex items-center">€</span>
            <input
                type="text"
                class="input input-bordered w-full"
                :class="{ 'input-error': modelValue === null }"
                v-model="displayValue"
                @keydown="onKeyDown"
                placeholder="0.00"
            />
        </div>

        <label v-if="modelValue === null" class="label">
            <span class="label-text-alt text-error">
                Please enter a valid price
            </span>
        </label>
    </div>
</template>