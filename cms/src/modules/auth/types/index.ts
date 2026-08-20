export interface UserInfo {
  id: string
  email: string
  fullName: string
  role: string
  avatar?: string
}

export interface LoginCredentials {
  email: string
  password: string
  remember?: boolean
}

export interface LoginResponseData {
  token: string
  user?: UserInfo
}

export interface AuthState {
  user: UserInfo | null
  token: string | null
  isAuthenticated: boolean
  isLoading: boolean
}
