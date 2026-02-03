<script setup lang="ts">
import { computed } from 'vue'

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
        return (props.modelValue / 100).toFixed(2)
    },
    set(value: string) {
        const normalized = value.replace(',', '.')
        const parsed = Number(normalized)

        if (isNaN(parsed) || parsed <= 0) {
            emit('update:modelValue', null)
            return
        }

        emit('update:modelValue', Math.round(parsed * 100))
    }
})
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