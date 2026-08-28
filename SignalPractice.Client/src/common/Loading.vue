<!-- 加載畫面 -->
<script setup></script>
<template>
  <div class="background">
    <div class="loader"></div>
  </div>
</template>
<style scoped>
/* HTML: <div class="loader"></div> */
.loader {
  width: 100px;
  aspect-ratio: 1;
  border-radius: 50%;
  background: #f03355;
  clip-path: polygon(0 0, 100% 0, 100% 100%, 0 100%);
  animation: l1 2s infinite cubic-bezier(0.3, 1, 0, 1);
}
@keyframes l1 {
  33% {
    border-radius: 0;
    background: #514b82;
    clip-path: polygon(0 0, 100% 0, 100% 100%, 0 100%);
  }
  66% {
    border-radius: 0;
    background: #ffa516;
    clip-path: polygon(50% 0, 50% 0, 100% 100%, 0 100%);
  }
}

.background {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background-color: rgba(155, 155, 155, 0.5); /* 半透明背景 */
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999; /* 確保其位於最上層 */
}
</style>
