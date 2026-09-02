import accountApiInstance from '@/api/accountInstance.js'

// 房間相關 API ===========================================================

// 查看房間資訊
export const getRoomInfoAPI = (roomId) =>
  accountApiInstance.get(`Room/GetRoomInfo`, { params: { roomCode: roomId } })
// 創建房間
export const createRoomAPI = (request) => accountApiInstance.post(`Room/CreateRoom`, request)
// 加入房間
export const joinRoomAPI = (roomCode) => accountApiInstance.post(`Room/JoinRoom`, roomCode)
// 離開房間
export const leaveRoomAPI = (roomId) =>
  accountApiInstance.delete(`Room/LeaveRoom`, { params: roomId })
