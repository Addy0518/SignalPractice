/*
   定義 pinia 的遊戲結算設定
*/
export const useGameResultStore = defineStore('gameResult', {
  state: () => ({ rankings: [] }),
  actions: {
    setRankings(rankings) {
      this.rankings = rankings
    },
    clear() {
      this.rankings = []
    },
  },
})
