<script setup>
import { ref } from 'vue'

/*
   變數名稱代表意義
   stageConfig : 設定白板（舞台）的寬高
   lines : 存放 " 畫完的 " 線條物件
   isDrawing : 判斷滑鼠是否正在按住並繪畫
   currentLine : 存放 " 正在畫的 " 線條物件
*/
const stageConfig = ref({
  // 用 window.innerWidth/Height 讓白板填滿整個視窗
  width: window.innerWidth,
  height: window.innerHeight,
})
const lines = ref([])
const isDrawing = ref(false)
const currentLine = ref([])

/*
  滑鼠按下時觸發 紀錄線的起始座標
*/
const handleMouseDown = (e) => {
  isDrawing.value = true
  // 取得滑鼠在 Stage 上的座標（getPointerPosition）
  const pos = e.target.getStage().getPointerPosition()
  // 把起始座標存進 currentLine
  currentLine.value = [pos.x, pos.y]
}

/*
   滑鼠移動時觸發 , 持續記錄線的座標
*/
const handleMouseMove = (e) => {
  if (!isDrawing.value) return
  const pos = e.target.getStage().getPointerPosition()
  // currentLine.value 是陣列 , 要攤平
  currentLine.value = [...currentLine.value, pos.x, pos.y]
}

/*
   滑鼠放開時觸發 , 把這條線存進 lines
*/
const handleMouseUp = () => {
  if (!isDrawing.value) return
  isDrawing.value = false
  lines.value.push({
    points: currentLine.value,
    stroke: 'black',
    strokeWidth: 3,
  })
  currentLine.value = []
}
</script>

<template>
  <!--
  Konva 元件介紹
-->
  <!--
      v-stage : 整個白板容器 ( 舞台 )
      config : 設定白板寬高
      @mousedown/@mousemove/@mouseup : 監聽滑鼠事件
  -->
  <v-stage
    :config="stageConfig"
    @mousedown="handleMouseDown"
    @mousemove="handleMouseMove"
    @mouseup="handleMouseUp"
  >
    <!--
        v-layer : 圖層
        所有形狀都必須再圖層裡 , 可以用多個 layer 圖層
    -->
    <v-layer>
      <!--
          v-line : 線條
          for 迴圈把每一條 " 以完成的 " 線條都渲染出來
          config 設定線條樣式
      -->
      <v-line
        v-for="(line, index) in lines"
        :key="index"
        :config="{
          // 座標陣列 [x1,y1,x2,y2,...]
          points: line.points,
          // 顏色
          stroke: line.stroke,
          // 寬度
          strokeWidth: line.strokeWidth,
          // 端點樣式
          lineCap: 'round',
          // 線條轉折樣式
          lineJoin: 'round',
        }"
      />
      <!--
          把 " 正在完成的 " 線條渲染出來
          if 當下有線條在渲染 , 樣式一樣
      -->
      <v-line
        v-if="currentLine.length > 0"
        :config="{
          points: currentLine,
          stroke: 'black',
          strokeWidth: 3,
          lineCap: 'round',
          lineJoin: 'round',
        }"
      />
    </v-layer>
  </v-stage>
</template>
