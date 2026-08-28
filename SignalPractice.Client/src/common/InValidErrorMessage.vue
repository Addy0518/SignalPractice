<script setup>
import { computed } from 'vue';

const props = defineProps({
  errorDto: {
    // 接收 vuelidate 的錯誤陣列
    type: Object,
    required: true,
  },
  vaildChiName: {
    // 接收欄位中文名稱
    type: String,
    required: false,
  },
});
// 把 props 包成 computed，讓模板可以直接用
const errorDto = computed(() => props.errorDto);
const vaildChiName = computed(() => props.vaildChiName);
</script>

<template>
  <!-- 迴圈讀取錯誤  -->
  <span
    v-for="error of errorDto"
    :key="error.$uid"
    class="animate__animated animate__headShake flex text-red-500 font-bold text-lg"
  >
    <!-- error.$message 是 validators.js 裡 withMessage 寫的文字 -->
    <!-- .replace('Value', vaildChiName) 把訊息裡的 'Value' 換成中文欄位名 -->
    <!-- 例如：'Value 為必填欄位' => '帳號 為必填欄位' -->
    {{ error.$message.replace('Value', vaildChiName) }}
  </span>
</template>
