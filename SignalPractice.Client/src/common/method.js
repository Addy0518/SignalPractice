/**
 * 取得 error400 的錯誤訊息
 * @param {Object} error400 API 回傳得 error400 物件
 * @returns {String} 錯誤訊息
 */
export const getError400Message = (error400) => {
  if (!error400) return '';
  return Object.values(error400).flat().join('\n');
};
