/*
   玩家隨機頭像
*/
const emojiList = [
  '🐶',
  '🐱',
  '🐰',
  '🦊',
  '🐼',
  '🐸',
  '🐵',
  '🐨',
  '🐯',
  '🦁',
  '🍕',
  '🍔',
  '🍩',
  '🍉',
  '🍓',
  '🍪',
]

/*
   拿取玩家隨機頭像
*/
export const getAvatarEmoji = (playerId) => {
  // Math.abs 取絕對值 ( 因為我的遊客登入的 userId 是負數 )
  // 並除以 emoji 陣列來拿到餘數 , 以此對應圖案

  // 舉例 : playerId 是 -20 , 現在 emoji 有 16 個 , 陣列就是 16
  // -20 取絕對值變 20 , 除 16 餘 4
  const index = Math.abs(playerId) % emojiList.length

  // 這裡就拿到剛剛的 4 並對應 emoji 陣列的第 4 個 emoji
  return emojiList[index]
}
