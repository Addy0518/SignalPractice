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
