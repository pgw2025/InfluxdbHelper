import { get } from './request'

export function getVariableSuggestions(query: string) {
  return get<string[]>('/variables/autocomplete', { query })
}
