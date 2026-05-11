import { type ReportConfig } from '../reportModel'

export const EDITOR_DRAFT_STORAGE_KEY = 'fluentreport-editor-draft-v1'

type EditorDraftPayload = {
  version: 1
  savedAt: string
  config: ReportConfig
}

type EditorProjectFilePayload = {
  kind: 'fluentreport-editor-project'
  version: 1
  exportedAt: string
  config: ReportConfig
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isValidFrame(value: unknown) {
  if (!isRecord(value)) {
    return false
  }

  return (
    typeof value.x === 'number'
    && Number.isFinite(value.x)
    && typeof value.y === 'number'
    && Number.isFinite(value.y)
    && typeof value.width === 'number'
    && Number.isFinite(value.width)
    && typeof value.height === 'number'
    && Number.isFinite(value.height)
  )
}

function isValidBlock(value: unknown) {
  if (!isRecord(value)) {
    return false
  }

  return (
    typeof value.id === 'string'
    && typeof value.pageId === 'string'
    && typeof value.type === 'string'
    && isValidFrame(value.frame)
  )
}

export function isValidReportConfig(value: unknown): value is ReportConfig {
  if (!isRecord(value)) {
    return false
  }

  const options = value.htmlRendererOptions
  const pages = value.pages
  const blocks = value.blocks
  const dataSources = value.dataSources

  return (
    typeof value.name === 'string'
    && typeof value.title === 'string'
    && typeof value.companyParam === 'string'
    && typeof value.periodParam === 'string'
    && Array.isArray(dataSources)
    && dataSources.every((source) => typeof source === 'string')
    && isRecord(options)
    && typeof options.maxWidth === 'number'
    && typeof options.fontFamily === 'string'
    && typeof options.pageDividerStyle === 'string'
    && typeof options.outlookCompatible === 'boolean'
    // showPageNumbers is optional for backward compatibility with saved drafts
    && (value.showPageNumbers === undefined || typeof value.showPageNumbers === 'boolean')
    && Array.isArray(pages)
    && pages.every((page) => isRecord(page) && typeof page.id === 'string')
    && Array.isArray(blocks)
    && blocks.every(isValidBlock)
  )
}

export function loadDraftConfig(): ReportConfig | null {
  if (typeof window === 'undefined') {
    return null
  }

  const raw = window.localStorage.getItem(EDITOR_DRAFT_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as EditorDraftPayload

    if (!isRecord(parsed) || parsed.version !== 1 || !isValidReportConfig(parsed.config)) {
      return null
    }

    return parsed.config
  } catch {
    return null
  }
}

export function getDraftSavedAt(): string | null {
  if (typeof window === 'undefined') {
    return null
  }

  const raw = window.localStorage.getItem(EDITOR_DRAFT_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as EditorDraftPayload
    if (!isRecord(parsed) || parsed.version !== 1 || typeof parsed.savedAt !== 'string') {
      return null
    }

    return parsed.savedAt
  } catch {
    return null
  }
}

export function saveDraftConfig(config: ReportConfig): string | null {
  if (typeof window === 'undefined') {
    return null
  }

  const savedAt = new Date().toISOString()
  const payload: EditorDraftPayload = {
    version: 1,
    savedAt,
    config,
  }

  window.localStorage.setItem(EDITOR_DRAFT_STORAGE_KEY, JSON.stringify(payload))

  return savedAt
}

export function clearDraftConfig() {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.removeItem(EDITOR_DRAFT_STORAGE_KEY)
}

export function createProjectFileContent(config: ReportConfig) {
  const payload: EditorProjectFilePayload = {
    kind: 'fluentreport-editor-project',
    version: 1,
    exportedAt: new Date().toISOString(),
    config,
  }

  return JSON.stringify(payload, null, 2)
}

export function parseProjectFileContent(content: string): ReportConfig | null {
  try {
    const parsed = JSON.parse(content) as EditorProjectFilePayload | ReportConfig

    if (isRecord(parsed) && 'kind' in parsed) {
      if (
        parsed.kind !== 'fluentreport-editor-project'
        || parsed.version !== 1
        || !isValidReportConfig(parsed.config)
      ) {
        return null
      }

      return parsed.config
    }

    if (isValidReportConfig(parsed)) {
      return parsed
    }

    return null
  } catch {
    return null
  }
}
