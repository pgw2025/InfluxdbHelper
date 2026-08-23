import { get, post } from './request'

export interface LoginResult {
  token: string
  expiresAt: string
  username: string
  displayName: string
}

export interface UserProfile {
  username: string
  displayName: string
}

export function login(username: string, password: string) {
  return post<LoginResult>('/auth/login', { username, password })
}

export function getProfile() {
  return get<UserProfile>('/auth/profile')
}
