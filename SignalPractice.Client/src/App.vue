<script setup>
//#region 提供控制 Toast 的方法給子組件
import { useToast } from 'primevue/usetoast';
import { setToast } from '@/common/toast';

const toast = useToast();
setToast(toast);
let loadingTimer = null;

//#region 提供控制 Loading 的方法給子組件
const isLoading = ref(false);

// 顯示 Loading
const showLoading = () => {
  isLoading.value = true;
};

// 關閉 Loading
const hideLoading = () => {
  clearTimeout(loadingTimer);
  loadingTimer = setTimeout(() => {
    isLoading.value = false;
  }, 500);
};

// 用 provide 把 Loading 的函式掛到全域
// 任何子元件都可以用 inject('showLoading') 拿到這些函式來用
provide('isLoading', isLoading);
provide('showLoading', showLoading);
provide('hideLoading', hideLoading);
//#endregion

// 成功提示（綠色）
// title = 標題, message = 內文, millisecond = 幾毫秒後自動消失（預設 3 秒）
const showToastSuccess = (title, message = '', millisecond = 3000) => {
  toast.add({ severity: 'success', summary: title, detail: message, life: millisecond });
};

// 資訊提示（藍色）
const showToastInfo = (title, message = '', millisecond = 3000) => {
  toast.add({ severity: 'info', summary: title, detail: message, life: millisecond });
};

// 警告提示（黃色）
const showToastWarn = (title, message = '', millisecond = 3000) => {
  toast.add({ severity: 'warn', summary: title, detail: message, life: millisecond });
};

// 錯誤提示（紅色）
const showToastError = (title, message = '', millisecond = 3000) => {
  toast.add({ severity: 'error', summary: title, detail: message, life: millisecond });
};

// 一樣給全域用
provide('showToastSuccess', showToastSuccess);
provide('showToastInfo', showToastInfo);
provide('showToastWarn', showToastWarn);
provide('showToastError', showToastError);
//#endregion
</script>

<template>
  <RouterView />
  <Loading v-if="isLoading" />
  <Toast position="top-center" />
</template>

