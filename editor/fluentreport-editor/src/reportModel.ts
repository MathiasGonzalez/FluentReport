export const PAGE_WIDTH = 794
export const PAGE_HEIGHT = 1123
export const PAGE_MARGIN = 56
export const PAGE_HEADER_Y = 38
export const PAGE_FOOTER_Y = PAGE_HEIGHT - 52

export const DEFAULT_PAGE_SETTINGS = {
  size: 'A4',
  orientation: 'portrait',
  margin: { top: 40, right: 40, bottom: 40, left: 40 },
} as const

export const DEFAULT_HTML_RENDERER_OPTIONS = {
  maxWidth: 600,
  fontFamily: 'Arial, Helvetica, sans-serif',
  pageDividerStyle: 'border-top: 2px dashed #cccccc; padding-top: 16px; padding-bottom: 16px',
  outlookCompatible: false,
} as const

export type Align = 'left' | 'center' | 'right' | 'justify'
export type ImageFit = 'contain' | 'cover' | 'none'
export type ImageSourceMode = 'path' | 'base64'
export type BlockType = 'text' | 'line' | 'spacer' | 'pageBreak' | 'image' | 'table' | 'repeat'
export type DataRegionGrowthMode = 'fixed' | 'grow'
export type DataRegionOverflowMode = 'nextPage' | 'truncate'

export type BlockFrame = {
  x: number
  y: number
  width: number
  height: number
}

export type BlockBase = {
  id: string
  pageId: string
  frame: BlockFrame
  groupId?: string
}

export type TextBlock = BlockBase & {
  id: string
  type: 'text'
  value: string
  styleRef?: string
  align?: Align
  fontSize?: number
  bold?: boolean
  italic?: boolean
  color?: string
}

export type LineBlock = BlockBase & {
  type: 'line'
  thickness: number
  color: string
}

export type SpacerBlock = BlockBase & {
  type: 'spacer'
  size: number
}

export type PageBreakBlock = BlockBase & {
  type: 'pageBreak'
}

export type ImageBlock = BlockBase & {
  type: 'image'
  source: string
  sourceMode: ImageSourceMode
  fit: ImageFit
  alt?: string
}

export type TableColumn = {
  id: string
  field: string
  header: string
  width: number
  align?: Align
}

export type TableBlock = BlockBase & {
  type: 'table'
  name: string
  dataSource: string
  columns: TableColumn[]
  growthMode: DataRegionGrowthMode
  overflowMode: DataRegionOverflowMode
  keepTogether: boolean
}

export type RepeatBlock = BlockBase & {
  type: 'repeat'
  name: string
  dataSource: string
  itemTemplate: string
  itemGap: number
  growthMode: DataRegionGrowthMode
  overflowMode: DataRegionOverflowMode
  keepTogether: boolean
}

export type Block = TextBlock | LineBlock | SpacerBlock | PageBreakBlock | ImageBlock | TableBlock | RepeatBlock

export type HtmlRendererOptions = {
  maxWidth: number
  fontFamily: string
  pageDividerStyle: string
  outlookCompatible: boolean
}

export type PageDefinition = {
  id: string
}

export type ReportConfig = {
  name: string
  title: string
  companyParam: string
  periodParam: string
  dataSources: string[]
  htmlRendererOptions: HtmlRendererOptions
  pages: PageDefinition[]
  blocks: Block[]
}

export function isAlign(value: string): value is Align {
  return value === 'left' || value === 'center' || value === 'right' || value === 'justify'
}

export function getSelectionBounds(blocks: Block[]) {
  if (blocks.length === 0) {
    return null
  }

  const left = Math.min(...blocks.map((block) => block.frame.x))
  const top = Math.min(...blocks.map((block) => block.frame.y))
  const right = Math.max(...blocks.map((block) => block.frame.x + block.frame.width))
  const bottom = Math.max(...blocks.map((block) => block.frame.y + block.frame.height))

  return {
    x: left,
    y: top,
    width: right - left,
    height: bottom - top,
  }
}