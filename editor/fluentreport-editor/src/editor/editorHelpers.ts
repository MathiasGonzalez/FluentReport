import { EDITOR_COPY, type Locale } from '../editorCopy'
import {
  DEFAULT_HTML_RENDERER_OPTIONS,
  PAGE_FOOTER_Y,
  PAGE_HEADER_Y,
  PAGE_HEIGHT,
  PAGE_MARGIN,
  PAGE_WIDTH,
  isAlign,
  type Block,
  type BlockFrame,
  type BlockType,
  type ReportConfig,
  type TableColumn,
} from '../reportModel'

export const CANVAS_VERTICAL_GUIDES = [0, PAGE_MARGIN, PAGE_WIDTH / 2, PAGE_WIDTH - PAGE_MARGIN, PAGE_WIDTH]
export const CANVAS_HORIZONTAL_GUIDES = [0, PAGE_HEADER_Y, PAGE_HEIGHT / 2, PAGE_FOOTER_Y, PAGE_HEIGHT]
export const ZOOM_LEVELS = [0.5, 0.75, 1, 1.25, 1.5, 2]

export const initialConfig: ReportConfig = {
  name: 'revenue-by-region',
  title: 'Revenue Report - {{ parameters.period }}',
  companyParam: 'companyName',
  periodParam: 'period',
  dataSources: ['sales'],
  htmlRendererOptions: { ...DEFAULT_HTML_RENDERER_OPTIONS },
  showPageNumbers: true,
  pages: [{ id: 'p1' }],
  blocks: [
    {
      id: 'b-1',
      pageId: 'p1',
      type: 'text',
      value: '{{ parameters.companyName }}',
      styleRef: 'h2',
      frame: { x: 72, y: 96, width: 280, height: 48 },
    },
    {
      id: 'b-2',
      pageId: 'p1',
      type: 'line',
      thickness: 1,
      color: '#D8D8D8',
      frame: { x: 72, y: 172, width: 650, height: 6 },
    },
    {
      id: 'b-3',
      pageId: 'p1',
      type: 'text',
      value: 'Revenue Report - {{ parameters.period }}',
      styleRef: 'title',
      align: 'center',
      frame: { x: 154, y: 214, width: 486, height: 72 },
    },
    {
      id: 'b-4',
      pageId: 'p1',
      type: 'spacer',
      size: 12,
      frame: { x: 72, y: 314, width: 220, height: 28 },
    },
    {
      id: 'b-5',
      pageId: 'p1',
      type: 'table',
      name: 'sales-table',
      dataSource: 'sales',
      growthMode: 'grow',
      overflowMode: 'nextPage',
      keepTogether: false,
      columns: [
        { id: 'col-region', field: 'region', header: 'Region', width: 2 },
        { id: 'col-month', field: 'month', header: 'Month', width: 2 },
        { id: 'col-revenue', field: 'revenue', header: 'Revenue', width: 1, align: 'right' },
      ],
      frame: { x: 72, y: 372, width: 520, height: 144 },
    },
  ],
}

let blockCounter = 100
let groupCounter = 1
let pageCounter = 1

export function nextBlockId() {
  blockCounter += 1
  return `b-${blockCounter}`
}

export function nextGroupId() {
  groupCounter += 1
  return `g-${groupCounter}`
}

export function nextPageId() {
  pageCounter += 1
  return `p${pageCounter}`
}

export function getBlockSelector(blockId: string) {
  return `.canvas-block[data-block-id="${blockId}"]`
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max)
}

export function createDefaultTableColumns(): TableColumn[] {
  return [
    { id: 'col-region', field: 'region', header: 'Region', width: 2 },
    { id: 'col-month', field: 'month', header: 'Month', width: 2 },
    { id: 'col-revenue', field: 'revenue', header: 'Revenue', width: 1, align: 'right' },
  ]
}

export function createDefaultRepeatTemplate() {
  return '{{ row.title }}\n{{ row.description }}'
}

export function formatTableColumns(columns: TableColumn[]): string {
  return columns
    .map((column) => [column.field, column.header, String(column.width), column.align ?? 'left'].join(' | '))
    .join('\n')
}

export function formatNameList(values: string[]): string {
  return values.join('\n')
}

export function parseNameList(input: string): string[] {
  return [...new Set(input.split(/\r?\n|,/).map((value) => value.trim()).filter(Boolean))]
}

export function parseTableColumns(input: string, previousColumns: TableColumn[]): TableColumn[] {
  const parsed = input
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean)
    .map((line, index) => {
      const [fieldRaw, headerRaw, widthRaw, alignRaw] = line.split('|').map((part) => part.trim())
      const field = fieldRaw || `column${index + 1}`
      const width = Number(widthRaw)

      return {
        id: previousColumns[index]?.id ?? `col-${field}`,
        field,
        header: headerRaw || field,
        width: Number.isFinite(width) && width > 0 ? width : 1,
        ...(alignRaw && isAlign(alignRaw) ? { align: alignRaw } : {}),
      }
    })

  return parsed.length > 0 ? parsed : previousColumns
}

function getMinimumFrame(type: BlockType) {
  if (type === 'line') {
    return { width: 80, height: 4 }
  }

  if (type === 'table') {
    return { width: 240, height: 120 }
  }

  if (type === 'repeat') {
    return { width: 220, height: 120 }
  }

  if (type === 'image') {
    return { width: 120, height: 80 }
  }

  if (type === 'spacer') {
    return { width: 80, height: 16 }
  }

  if (type === 'pageBreak') {
    return { width: 120, height: 40 }
  }

  return { width: 120, height: 40 }
}

export function clampFrame(frame: BlockFrame, type: BlockType): BlockFrame {
  const minimum = getMinimumFrame(type)
  const width = clamp(frame.width, minimum.width, PAGE_WIDTH)
  const height = clamp(frame.height, minimum.height, PAGE_HEIGHT)

  return {
    width,
    height,
    x: clamp(frame.x, 0, PAGE_WIDTH - width),
    y: clamp(frame.y, 0, PAGE_HEIGHT - height),
  }
}

export function createBlock(type: BlockType, index: number, pageId: string): Block {
  const baseFrame = clampFrame(
    {
      x: 72,
      y: 96 + index * 84,
      width:
        type === 'line'
          ? 520
          : type === 'table'
            ? 420
            : type === 'repeat'
              ? 320
              : type === 'pageBreak'
                ? 180
                : type === 'spacer'
                  ? 220
                  : type === 'image'
                    ? 240
                    : 260,
      height:
        type === 'line'
          ? 6
          : type === 'spacer'
            ? 28
            : type === 'pageBreak'
              ? 40
              : type === 'image'
                ? 160
                : type === 'table'
                  ? 148
                  : type === 'repeat'
                    ? 160
                    : 72,
    },
    type,
  )

  if (type === 'text') {
    return { id: nextBlockId(), pageId, type: 'text', value: 'New text block', frame: baseFrame }
  }

  if (type === 'line') {
    return { id: nextBlockId(), pageId, type: 'line', thickness: 1, color: '#D8D8D8', frame: baseFrame }
  }

  if (type === 'spacer') {
    return { id: nextBlockId(), pageId, type: 'spacer', size: 10, frame: baseFrame }
  }

  if (type === 'image') {
    return { id: nextBlockId(), pageId, type: 'image', source: '', sourceMode: 'path', fit: 'contain', alt: 'Image', frame: baseFrame }
  }

  if (type === 'table') {
    return {
      id: nextBlockId(),
      pageId,
      type: 'table',
      name: 'table-block',
      dataSource: 'items',
      growthMode: 'grow',
      overflowMode: 'nextPage',
      keepTogether: false,
      columns: createDefaultTableColumns(),
      frame: baseFrame,
    }
  }

  if (type === 'repeat') {
    return {
      id: nextBlockId(),
      pageId,
      type: 'repeat',
      name: 'repeat-block',
      dataSource: 'items',
      itemTemplate: createDefaultRepeatTemplate(),
      itemGap: 10,
      growthMode: 'grow',
      overflowMode: 'nextPage',
      keepTogether: false,
      frame: baseFrame,
    }
  }

  return { id: nextBlockId(), pageId, type: 'pageBreak', frame: baseFrame }
}

export function getBlockTypeLabel(type: BlockType, locale: Locale): string {
  return EDITOR_COPY[locale].blockTypes[type]
}

function getGrowthModeLabel(mode: 'fixed' | 'grow', locale: Locale) {
  const copy = EDITOR_COPY[locale]
  return mode === 'grow' ? copy.growDown : copy.fixed
}

function getOverflowModeLabel(mode: 'nextPage' | 'truncate', locale: Locale) {
  const copy = EDITOR_COPY[locale]
  return mode === 'nextPage' ? copy.nextPage : copy.truncate
}

export function getBlockSummary(block: Block, locale: Locale): string {
  const copy = EDITOR_COPY[locale]

  if (block.type === 'text') {
    return block.value || copy.emptyTextBlock
  }

  if (block.type === 'line') {
    return `${block.thickness}px · ${block.color}`
  }

  if (block.type === 'spacer') {
    return copy.spacerSummary(block.size)
  }

  if (block.type === 'image') {
    return block.source ? block.alt || block.source : copy.emptyImageBlock
  }

  if (block.type === 'table') {
    return `${block.dataSource} · ${copy.columnsCount(block.columns.length)} · ${getGrowthModeLabel(block.growthMode, locale)} / ${getOverflowModeLabel(block.overflowMode, locale)}`
  }

  if (block.type === 'repeat') {
    return `${block.dataSource} · ${copy.gap} ${block.itemGap} · ${getGrowthModeLabel(block.growthMode, locale)} / ${getOverflowModeLabel(block.overflowMode, locale)}`
  }

  return copy.forceNewPage
}

export function expandSelectionForBlocks(blocks: Block[], blockIds: string[], primaryId?: string | null) {
  if (blockIds.length === 0) {
    return []
  }

  const directIds = new Set(blockIds)
  const groupIds = new Set(
    blocks
      .filter((block) => directIds.has(block.id) && block.groupId)
      .map((block) => block.groupId as string),
  )
  const orderedIds = blocks
    .filter((block) => directIds.has(block.id) || (block.groupId && groupIds.has(block.groupId)))
    .map((block) => block.id)

  if (primaryId && orderedIds.includes(primaryId)) {
    return [...orderedIds.filter((id) => id !== primaryId), primaryId]
  }

  return orderedIds
}

function getGroupBlockIds(blocks: Block[], groupId: string) {
  return blocks.filter((block) => block.groupId === groupId).map((block) => block.id)
}

export function isExactGroupSelection(blocks: Block[], selectedIds: string[], groupId: string) {
  const groupBlockIds = getGroupBlockIds(blocks, groupId)

  return (
    groupBlockIds.length > 1
    && groupBlockIds.length === selectedIds.length
    && groupBlockIds.every((id) => selectedIds.includes(id))
  )
}

export function isTextEntryTarget(target: EventTarget | null) {
  if (!(target instanceof HTMLElement)) {
    return false
  }

  return Boolean(target.closest('input, textarea, select, [contenteditable="true"]'))
}

export function getNextZoom(currentZoom: number, direction: -1 | 1): number {
  const currentIndex = ZOOM_LEVELS.findIndex((level) => level >= currentZoom)
  const safeIndex = currentIndex === -1 ? ZOOM_LEVELS.length - 1 : currentIndex
  const nextIndex = clamp(safeIndex + direction, 0, ZOOM_LEVELS.length - 1)

  return ZOOM_LEVELS[nextIndex]
}
