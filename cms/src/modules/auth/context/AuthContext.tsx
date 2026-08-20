import React, { createContext, useContext, useState, useEffect, useCallback, useMemo } from 'react'
import { authService } from '../services/authService'
import type { LoginCredentials, UserInfo } from '../types'

export interface AuthContextType {
  user: UserInfo | null
  token: string | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (credentials: LoginCredentials) => Promise<UserInfo>
  logout: () => Promise<void>
  refreshProfile: () => Promise<UserInfo | null>
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserInfo | null>(() => authService.getCurrentUser())
  const [token, setToken] = useState<string | null>(() => authService.getToken())
  const [isLoading, setIsLoading] = useState<boolean>(true)

  const isAuthenticated = Boolean(token)

  // Initialize auth state
  useEffect(() => {
    const initAuth = async () => {
      const storedToken = authService.getToken()
      if (storedToken) {
        setToken(storedToken)
        const storedUser = authService.getCurrentUser()
        if (storedUser) {
          setUser(storedUser)
        }
        try {
          const profile = await authService.getProfile()
          setUser(profile)
        } catch {
          // Token might be invalid, or backend offline
        }
      } else {
        setUser(null)
        setToken(null)
      }
      setIsLoading(false)
    }

    initAuth()
  }, [])

  const login = useCallback(async (credentials: LoginCredentials): Promise<UserInfo> => {
    setIsLoading(true)
    try {
      const result = await authService.login(credentials)
      setToken(result.token)
      if (result.user) {
        setUser(result.user)
        return result.user
      }
      const profile = await authService.getProfile()
      setUser(profile)
      return profile
    } finally {
      setIsLoading(false)
    }
  }, [])

  const logout = useCallback(async () => {
    setIsLoading(true)
    try {
      await authService.logout()
    } finally {
      setUser(null)
      setToken(null)
      setIsLoading(false)
    }
  }, [])

  const refreshProfile = useCallback(async (): Promise<UserInfo | null> => {
    try {
      const profile = await authService.getProfile()
      setUser(profile)
      return profile
    } catch {
      return null
    }
  }, [])

  const contextValue = useMemo<AuthContextType>(
    () => ({
      user,
      token,
      isAuthenticated,
      isLoading,
      login,
      logout,
      refreshProfile,
    }),
    [user, token, isAuthenticated, isLoading, login, logout, refreshProfile]
  )

  return <AuthContext.Provider value={contextValue}>{children}</AuthContext.Provider>
}

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

