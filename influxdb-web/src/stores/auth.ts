import { defineStore } from 'pinia'
import { login, getProfile, type UserProfile } from '@/api/auth'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('token') || '',
    displayName: localStorage.getItem('displayName') || ''
  }),
  getters: {
    isLoggedIn: state => !!state.token
  },
  actions: {
    async doLogin(username: string, password: string) {
      const res = await login(username, password)
      this.token = res.token
      this.displayName = res.displayName
      localStorage.setItem('token', res.token)
      localStorage.setItem('displayName', res.displayName)
    },
    logout() {
      this.token = ''
      this.displayName = ''
      localStorage.removeItem('token')
      localStorage.removeItem('displayName')
    },
    async refreshProfile(): Promise<UserProfile | null> {
      if (!this.token) return null
      try {
        return await getProfile()
      } catch {
        return null
      }
    }
  }
})
