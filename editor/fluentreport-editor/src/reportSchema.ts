import {
  DEFAULT_PAGE_SETTINGS,
  PAGE_FOOTER_Y,
  PAGE_HEADER_Y,
  PAGE_HEIGHT,
  PAGE_MARGIN,
  PAGE_WIDTH,
  getSelectionBounds,
  type Align,
  type Block,
  type BlockFrame,
  type DataRegionGrowthMode,
  type DataRegionOverflowMode,
  type ImageFit,
  type ImageSourceMode,
  type ReportConfig,
} from './reportModel'

type SchemaFrame = {
  x: number
  y: number
  width: number
  height: number
}

type SchemaTextRun =
  | {
      value: string
    }
  | {
      token: 'currentPage' | 'totalPages'
    }

type SchemaTextNode = {
  id: string
  type: 'text'
  frame: SchemaFrame
  zIndex: number
  value?: string
  runs?: SchemaTextRun[]
  styleRef?: string
  align?: Align
  fontSize?: number
  bold?: boolean
  italic?: boolean
  color?: string
}

type SchemaLineNode = {
  id: string
  type: 'line'
  frame: SchemaFrame
  zIndex: number
  thickness: number
  color: string
}

type SchemaSpacerNode = {
  id: string
  type: 'spacer'
  frame: SchemaFrame
  zIndex: number
  size: number
}

type SchemaPageBreakNode = {
  id: string
  type: 'pageBreak'
  frame: SchemaFrame
  zIndex: number
}

type SchemaImageNode = {
  id: string
  type: 'image'
  frame: SchemaFrame
  zIndex: number
  source: {
    mode: ImageSourceMode
    value: string
  }
  fit?: ImageFit
  alt?: string
}

type SchemaTableColumn = {
  field: string
  header: string
  width: number
  align?: Align
}

type SchemaTableNode = {
  id: string
  type: 'table'
  frame: SchemaFrame
  zIndex: number
  name: string
  dataSource: string
  definitionRef: string
  columns: SchemaTableColumn[]
  growthMode: DataRegionGrowthMode
  overflowMode: DataRegionOverflowMode
  keepTogether: boolean
}

type SchemaRepeatNode = {
  id: string
  type: 'repeat'
  frame: SchemaFrame
  zIndex: number
  name: string
  dataSource: string
  definitionRef: string
  itemTemplate: string
  itemGap: number
  growthMode: DataRegionGrowthMode
  overflowMode: DataRegionOverflowMode
  keepTogether: boolean
}

type SchemaTableRepeatableDefinition = {
  id: string
  type: 'table'
  name: string
  dataSource: string
  columns: SchemaTableColumn[]
  growthMode: DataRegionGrowthMode
  overflowMode: DataRegionOverflowMode
  keepTogether: boolean
}

type SchemaRepeatDefinition = {
  id: string
  type: 'repeat'
  name: string
  dataSource: string
  itemTemplate: string
  itemGap: number
  growthMode: DataRegionGrowthMode
  overflowMode: DataRegionOverflowMode
  keepTogether: boolean
}

type SchemaBlockNode = SchemaTextNode | SchemaLineNode | SchemaSpacerNode | SchemaPageBreakNode | SchemaImageNode | SchemaTableNode | SchemaRepeatNode

type SchemaGroupInstanceNode = {
  id: string
  type: 'groupInstance'
  groupRef: string
  frame: SchemaFrame
  zIndex: number
}

type SchemaNode = SchemaBlockNode | SchemaGroupInstanceNode

type SchemaRegion = {
  frame: SchemaFrame
  nodes: SchemaNode[]
}

function sortBlocksForSchema(blocks: Block[]): Block[] {
  return [...blocks].sort((left, right) => {
    if (left.frame.y !== right.frame.y) {
      return left.frame.y - right.frame.y
    }

    return left.frame.x - right.frame.x
  })
}

function serializeFrame(frame: BlockFrame): SchemaFrame {
  return {
    x: frame.x,
    y: frame.y,
    width: frame.width,
    height: frame.height,
  }
}

function getPageRegions(): { header: SchemaRegion; content: SchemaRegion; footer: SchemaRegion } {
  return {
    header: {
      frame: {
        x: PAGE_MARGIN,
        y: 0,
        width: PAGE_WIDTH - PAGE_MARGIN * 2,
        height: PAGE_HEADER_Y,
      },
      nodes: [],
    },
    content: {
      frame: {
        x: PAGE_MARGIN,
        y: PAGE_HEADER_Y,
        width: PAGE_WIDTH - PAGE_MARGIN * 2,
        height: PAGE_FOOTER_Y - PAGE_HEADER_Y,
      },
      nodes: [],
    },
    footer: {
      frame: {
        x: PAGE_MARGIN,
        y: PAGE_FOOTER_Y,
        width: PAGE_WIDTH - PAGE_MARGIN * 2,
        height: PAGE_HEIGHT - PAGE_FOOTER_Y,
      },
      nodes: [],
    },
  }
}

function buildPageRegions(blocks: Block[], showPageNumbers: boolean) {
  const zIndexById = new Map(blocks.map((block, index) => [block.id, index]))
  const emittedGroups = new Set<string>()
  const regions = getPageRegions()

  regions.content.nodes = blocks.flatMap<SchemaNode>((block) => {
    if (!block.groupId) {
      return [serializeBlockNode(block, zIndexById.get(block.id) ?? 0)]
    }

    if (emittedGroups.has(block.groupId)) {
      return []
    }

    emittedGroups.add(block.groupId)

    const groupBlocks = blocks.filter((candidate) => candidate.groupId === block.groupId)
    const groupBounds = getSelectionBounds(groupBlocks)

    if (!groupBounds) {
      return []
    }

    return [
      {
        id: `instance-${block.groupId}`,
        type: 'groupInstance',
        groupRef: block.groupId,
        frame: serializeFrame(groupBounds),
        zIndex: Math.min(...groupBlocks.map((candidate) => zIndexById.get(candidate.id) ?? 0)),
      },
    ]
  })

  regions.footer.nodes = showPageNumbers
    ? [
        {
          id: 'footer-pagination',
          type: 'text',
          frame: serializeFrame(regions.footer.frame),
          zIndex: 0,
          align: 'center',
          runs: [
            { value: 'Page ' },
            { token: 'currentPage' },
            { value: ' of ' },
            { token: 'totalPages' },
          ],
        },
      ]
    : []

  return regions
}

function serializeBlockNode(block: Block, zIndex: number, frame = block.frame): SchemaBlockNode {
  if (block.type === 'text') {
    return {
      id: block.id,
      type: 'text',
      frame: serializeFrame(frame),
      zIndex,
      value: block.value,
      ...(block.styleRef ? { styleRef: block.styleRef } : {}),
      ...(block.align ? { align: block.align } : {}),
      ...(block.fontSize ? { fontSize: block.fontSize } : {}),
      ...(block.bold ? { bold: block.bold } : {}),
      ...(block.italic ? { italic: block.italic } : {}),
      ...(block.color ? { color: block.color } : {}),
    }
  }

  if (block.type === 'line') {
    return {
      id: block.id,
      type: 'line',
      frame: serializeFrame(frame),
      zIndex,
      thickness: block.thickness,
      color: block.color,
    }
  }

  if (block.type === 'spacer') {
    return {
      id: block.id,
      type: 'spacer',
      frame: serializeFrame(frame),
      zIndex,
      size: block.size,
    }
  }

  if (block.type === 'image') {
    return {
      id: block.id,
      type: 'image',
      frame: serializeFrame(frame),
      zIndex,
      source: {
        mode: block.sourceMode,
        value: block.source,
      },
      ...(block.fit ? { fit: block.fit } : {}),
      ...(block.alt ? { alt: block.alt } : {}),
    }
  }

  if (block.type === 'table') {
    return {
      id: block.id,
      type: 'table',
      frame: serializeFrame(frame),
      zIndex,
      name: block.name,
      dataSource: block.dataSource,
      definitionRef: `table-${block.id}`,
      columns: block.columns.map((column) => ({
        field: column.field,
        header: column.header,
        width: column.width,
        ...(column.align ? { align: column.align } : {}),
      })),
      growthMode: block.growthMode,
      overflowMode: block.overflowMode,
      keepTogether: block.keepTogether,
    }
  }

  if (block.type === 'repeat') {
    return {
      id: block.id,
      type: 'repeat',
      frame: serializeFrame(frame),
      zIndex,
      name: block.name,
      dataSource: block.dataSource,
      definitionRef: `repeat-${block.id}`,
      itemTemplate: block.itemTemplate,
      itemGap: block.itemGap,
      growthMode: block.growthMode,
      overflowMode: block.overflowMode,
      keepTogether: block.keepTogether,
    }
  }

  return {
    id: block.id,
    type: 'pageBreak',
    frame: serializeFrame(frame),
    zIndex,
  }
}

function buildRepeatableDefinitions(blocks: Block[]): Array<SchemaTableRepeatableDefinition | SchemaRepeatDefinition> {
  return blocks.flatMap<SchemaTableRepeatableDefinition | SchemaRepeatDefinition>((block) => {
    if (block.type === 'table') {
      return [
        {
          id: `table-${block.id}`,
          type: 'table',
          name: block.name,
          dataSource: block.dataSource,
          columns: block.columns.map((column) => ({
            field: column.field,
            header: column.header,
            width: column.width,
            ...(column.align ? { align: column.align } : {}),
          })),
          growthMode: block.growthMode,
          overflowMode: block.overflowMode,
          keepTogether: block.keepTogether,
        },
      ]
    }

    if (block.type === 'repeat') {
      return [
        {
          id: `repeat-${block.id}`,
          type: 'repeat',
          name: block.name,
          dataSource: block.dataSource,
          itemTemplate: block.itemTemplate,
          itemGap: block.itemGap,
          growthMode: block.growthMode,
          overflowMode: block.overflowMode,
          keepTogether: block.keepTogether,
        },
      ]
    }

    return []
  })
}

function buildGroupDefinitions(blocks: Block[], zIndexById: Map<string, number>) {
  const groups = new Map<string, Block[]>()

  blocks.forEach((block) => {
    if (!block.groupId) {
      return
    }

    const currentGroup = groups.get(block.groupId) ?? []
    currentGroup.push(block)
    groups.set(block.groupId, currentGroup)
  })

  return [...groups.entries()].map(([groupId, groupBlocks]) => {
    const groupBounds = getSelectionBounds(groupBlocks)

    if (!groupBounds) {
      return {
        id: groupId,
        type: 'group',
        name: `Group ${groupId}`,
        frame: { x: 0, y: 0, width: 0, height: 0 },
        nodes: [],
      }
    }

    const orderedGroupBlocks = sortBlocksForSchema(groupBlocks)

    return {
      id: groupId,
      type: 'group',
      name: `Group ${groupId}`,
      frame: {
        x: 0,
        y: 0,
        width: groupBounds.width,
        height: groupBounds.height,
      },
      nodes: orderedGroupBlocks.map((block) =>
        serializeBlockNode(block, zIndexById.get(block.id) ?? 0, {
          x: block.frame.x - groupBounds.x,
          y: block.frame.y - groupBounds.y,
          width: block.frame.width,
          height: block.frame.height,
        }),
      ),
    }
  })
}

function quoteIfNeeded(value: string): string {
  if (value.length === 0) {
    return '""'
  }

  if (/^[-?:@`#!&*|>'"%!{},[\]]/.test(value) || value.includes(': ') || /\s/.test(value)) {
    return `"${value.replace(/"/g, '\\"')}"`
  }

  return value
}

export function toYaml(value: unknown, depth = 0): string {
  const indent = '  '.repeat(depth)

  if (value === null || value === undefined) {
    return 'null'
  }

  if (typeof value === 'string') {
    return quoteIfNeeded(value)
  }

  if (typeof value === 'number' || typeof value === 'boolean') {
    return String(value)
  }

  if (Array.isArray(value)) {
    if (value.length === 0) {
      return '[]'
    }

    return value
      .map((item) => {
        if (typeof item === 'object' && item !== null) {
          const nested = toYaml(item, depth + 1)
          const lines = nested.split('\n')
          const [first, ...rest] = lines
          const head = `${indent}- ${first}`
          const tail = rest.map((line) => `${indent}  ${line}`).join('\n')
          return tail ? `${head}\n${tail}` : head
        }

        return `${indent}- ${toYaml(item, depth + 1)}`
      })
      .join('\n')
  }

  const entries = Object.entries(value as Record<string, unknown>).filter(([, v]) => v !== undefined)

  if (entries.length === 0) {
    return '{}'
  }

  return entries
    .map(([key, val]) => {
      if (val !== null && typeof val === 'object') {
        if (Array.isArray(val) && val.length === 0) {
          return `${indent}${key}: []`
        }

        const nested = toYaml(val, depth + 1)
        return `${indent}${key}:\n${nested}`
      }

      return `${indent}${key}: ${toYaml(val, depth + 1)}`
    })
    .join('\n')
}

export function buildSchema(config: ReportConfig) {
  const zIndexById = new Map(config.blocks.map((block, index) => [block.id, index]))
  const groupDefinitions = buildGroupDefinitions(config.blocks, zIndexById)
  const repeatableDefinitions = buildRepeatableDefinitions(config.blocks)

  return {
    kind: 'FluentReport',
    schemaVersion: 1,
    name: config.name,
    metadata: {
      title: config.title,
    },
    pageDefaults: DEFAULT_PAGE_SETTINGS,
    parameters: {
      [config.companyParam]: { type: 'string', required: true },
      [config.periodParam]: { type: 'string', required: true },
    },
    dataSources: Object.fromEntries(config.dataSources.map((name) => [name, { type: 'array' }])),
    assets: {},
    styles: {
      title: { fontSize: 20, bold: true, align: 'center' },
      h2: { fontSize: 13, bold: true },
    },
    rendererOptions: {
      html: config.htmlRendererOptions,
    },
    definitions: {
      groups: groupDefinitions,
      repeatables: repeatableDefinitions,
    },
    pages: config.pages.map((page) => ({
      id: page.id,
      ...DEFAULT_PAGE_SETTINGS,
      regions: buildPageRegions(
        config.blocks.filter((block) => block.pageId === page.id),
        config.showPageNumbers !== false,
      ),
    })),
  }
}