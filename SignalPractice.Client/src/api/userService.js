import accountApiInstance from '@/api/accountInstance.js'

// 使用者相關 API ===========================================================

/// <summary>
/// 使用者註冊
/// </summary>
export const registerAPI = (data) => accountApiInstance.post('/User/Register', data)

/// <summary>
/// 使用者登入
/// </summary>
export const loginAPI = (data) => accountApiInstance.post('/User/Login', data)

/// <summary>
/// 遊客登入
/// </summary>
export const guestLoginAPI = () => accountApiInstance.post('/User/GuestLogin')

/// <summary>
/// 查看個人歷史紀錄
/// </summary>
export const getGameRecordInfoAPI = () => accountApiInstance.get('/User/GetGameRecordInfo')

/// <summary>
/// 查看歷史紀錄的詳細資訊
/// </summary>
export const getRecordDetailsInfoAPI = (gameRecordId) =>
  accountApiInstance.get(`/User/GetRecordDetailsInfo`, { params: { gameRecordId } })
