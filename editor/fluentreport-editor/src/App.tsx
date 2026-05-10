import Guides from '@scena/guides'
import { useEffect, useMemo, useRef, useState } from 'react'
import Moveable from 'react-moveable'
import Selecto from 'react-selecto'
import './App.css'

const PAGE_WIDTH = 794
const PAGE_HEIGHT = 1123
const PAGE_MARGIN = 56
const PAGE_HEADER_Y = 38
const PAGE_FOOTER_Y = PAGE_HEIGHT - 52

const CANVAS_VERTICAL_GUIDES = [0, PAGE_MARGIN, PAGE_WIDTH / 2, PAGE_WIDTH - PAGE_MARGIN, PAGE_WIDTH]
const CANVAS_HORIZONTAL_GUIDES = [0, PAGE_HEADER_Y, PAGE_HEIGHT / 2, PAGE_FOOTER_Y, PAGE_HEIGHT]
const ZOOM_LEVELS = [0.5, 0.75, 1, 1.25, 1.5, 2]

type Align = 'left' | 'center' | 'right' | 'justify'
type ImageFit = 'contain' | 'cover' | 'none'
type ImageSourceMode = 'path' | 'base64'
type BlockType = 'text' | 'line' | 'spacer' | 'pageBreak' | 'image'

type BlockFrame = {
  x: number
  y: number
  width: number
  height: number
}

type BlockBase = {
  id: string
  frame: BlockFrame
  groupId?: string
}

type TextBlock = BlockBase & {
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

type LineBlock = BlockBase & {
  type: 'line'
  thickness: number
  color: string
}

type SpacerBlock = BlockBase & {
  type: 'spacer'
  size: number
}

type PageBreakBlock = BlockBase & {
  type: 'pageBreak'
}

type ImageBlock = BlockBase & {
  type: 'image'
  source: string
  sourceMode: ImageSourceMode
  fit: ImageFit
  alt?: string
}

type Block = TextBlock | LineBlock | SpacerBlock | PageBreakBlock | ImageBlock

type ReportConfig = {
  name: string
  title: string
  companyParam: string
  periodParam: string
  blocks: Block[]
}

type GuidesInstance = Guides & {
  scroll: (pos: number, nextZoom?: number) => void
  zoomTo: (nextZoom: number, nextGuidesZoom?: number) => void
  resize: (nextZoom?: number) => void
}

const initialConfig: ReportConfig = {
  name: 'revenue-by-region',
  title: 'Revenue Report - {{ parameters.period }}',
  companyParam: 'companyName',
  periodParam: 'period',
  blocks: [
    {
      id: 'b-1',
      type: 'text',
      value: '{{ parameters.companyName }}',
      styleRef: 'h2',
      frame: { x: 72, y: 96, width: 280, height: 48 },
    },
    {
      id: 'b-2',
      type: 'line',
      thickness: 1,
      color: '#D8D8D8',
      frame: { x: 72, y: 172, width: 650, height: 6 },
    },
    {
      id: 'b-3',
      type: 'text',
      value: 'Revenue Report - {{ parameters.period }}',
      styleRef: 'title',
      align: 'center',
      frame: { x: 154, y: 214, width: 486, height: 72 },
    },
    {
      id: 'b-4',
      type: 'spacer',
      size: 12,
      frame: { x: 72, y: 314, width: 220, height: 28 },
    },
    {
      id: 'b-5',
      type: 'text',
      value: 'Table placeholder: define table in next iteration of the editor.',
      frame: { x: 72, y: 372, width: 520, height: 108 },
    },
  ],
}

let idCounter = 100
let groupCounter = 1

function nextId() {
  idCounter += 1
  return `b-${idCounter}`
}

function nextGroupId() {
  groupCounter += 1
  return `g-${groupCounter}`
}

function getBlockSelector(blockId: string) {
  return `.canvas-block[data-block-id="${blockId}"]`
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max)
}

function getMinimumFrame(type: BlockType) {
  if (type === 'line') {
    return { width: 80, height: 4 }
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

function clampFrame(frame: BlockFrame, type: BlockType): BlockFrame {
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

function createBlock(type: BlockType, index: number): Block {
  const baseFrame = clampFrame(
    {
      x: 72,
      y: 96 + index * 84,
      width:
        type === 'line'
          ? 520
          : type === 'pageBreak'
            ? 180
            : type === 'spacer'
              ? 220
              : type === 'image'
                ? 240
                : 260,
      height: type === 'line' ? 6 : type === 'spacer' ? 28 : type === 'pageBreak' ? 40 : type === 'image' ? 160 : 72,
    },
    type,
  )

  if (type === 'text') {
    return { id: nextId(), type: 'text', value: 'New text block', frame: baseFrame }
  }

  if (type === 'line') {
    return { id: nextId(), type: 'line', thickness: 1, color: '#D8D8D8', frame: baseFrame }
  }

  if (type === 'spacer') {
    return { id: nextId(), type: 'spacer', size: 10, frame: baseFrame }
  }

  if (type === 'image') {
    return { id: nextId(), type: 'image', source: '', sourceMode: 'path', fit: 'contain', alt: 'Image', frame: baseFrame }
  }

  return { id: nextId(), type: 'pageBreak', frame: baseFrame }
}

function getBlockTypeLabel(type: BlockType): string {
  if (type === 'image') {
    return 'Image'
  }

  if (type === 'pageBreak') {
    return 'Page break'
  }

  return type.charAt(0).toUpperCase() + type.slice(1)
}

function getBlockSummary(block: Block): string {
  if (block.type === 'text') {
    return block.value || 'Empty text block'
  }

  if (block.type === 'line') {
    return `${block.thickness}px · ${block.color}`
  }

  if (block.type === 'spacer') {
    return `${block.size}px spacer`
  }

  if (block.type === 'image') {
    return block.source ? block.alt || block.source : 'Empty image block'
  }

  return 'Forces a new page'
}

function expandSelectionForBlocks(blocks: Block[], blockIds: string[], primaryId?: string | null) {
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

function getSelectionBounds(blocks: Block[]) {
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

function isTextEntryTarget(target: EventTarget | null) {
  if (!(target instanceof HTMLElement)) {
    return false
  }

  return Boolean(target.closest('input, textarea, select, [contenteditable="true"]'))
}

function getNextZoom(currentZoom: number, direction: -1 | 1): number {
  const currentIndex = ZOOM_LEVELS.findIndex((level) => level >= currentZoom)
  const safeIndex = currentIndex === -1 ? ZOOM_LEVELS.length - 1 : currentIndex
  const nextIndex = clamp(safeIndex + direction, 0, ZOOM_LEVELS.length - 1)

  return ZOOM_LEVELS[nextIndex]
}

function sortBlocksForSchema(blocks: Block[]): Block[] {
  return [...blocks].sort((left, right) => {
    if (left.frame.y !== right.frame.y) {
      return left.frame.y - right.frame.y
    }

    return left.frame.x - right.frame.x
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

function toYaml(value: unknown, depth = 0): string {
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

function buildSchema(config: ReportConfig) {
  const orderedBlocks = sortBlocksForSchema(config.blocks)

  return {
    kind: 'FluentReport',
    schemaVersion: 1,
    name: config.name,
    pageDefaults: {
      size: 'A4',
      orientation: 'portrait',
      margin: { top: 40, right: 40, bottom: 40, left: 40 },
    },
    parameters: {
      [config.companyParam]: { type: 'string', required: true },
      [config.periodParam]: { type: 'string', required: true },
    },
    styles: {
      title: { fontSize: 20, bold: true, align: 'center' },
      h2: { fontSize: 13, bold: true },
    },
    pages: [
      {
        id: 'p1',
        content: {
          type: 'column',
          spacing: 10,
          items: orderedBlocks.map((b) => {
            if (b.type === 'text') {
              return {
                type: 'text',
                value: b.value,
                ...(b.styleRef ? { styleRef: b.styleRef } : {}),
                ...(b.align ? { align: b.align } : {}),
                ...(b.fontSize ? { fontSize: b.fontSize } : {}),
                ...(b.bold ? { bold: b.bold } : {}),
                ...(b.italic ? { italic: b.italic } : {}),
                ...(b.color ? { color: b.color } : {}),
              }
            }

            if (b.type === 'line') {
              return {
                type: 'line',
                thickness: b.thickness,
                color: b.color,
              }
            }

            if (b.type === 'spacer') {
              return {
                type: 'spacer',
                size: b.size,
              }
            }

            if (b.type === 'image') {
              return {
                type: 'image',
                source: {
                  mode: b.sourceMode,
                  value: b.source,
                },
                ...(b.fit ? { fit: b.fit } : {}),
                ...(b.alt ? { alt: b.alt } : {}),
              }
            }

            return {
              type: 'pageBreak',
            }
          }),
        },
        footer: {
          type: 'text',
          align: 'center',
          runs: [
            { value: 'Page ' },
            { token: 'currentPage' },
            { value: ' of ' },
            { token: 'totalPages' },
          ],
        },
      },
    ],
  }
}

function App() {
  const [config, setConfig] = useState<ReportConfig>(initialConfig)
  const [selectedIds, setSelectedIds] = useState<string[]>(initialConfig.blocks[0] ? [initialConfig.blocks[0].id] : [])
  const [canvasZoom, setCanvasZoom] = useState(1)
  const [isInspectorOpen, setIsInspectorOpen] = useState(false)
  const [showRulers, setShowRulers] = useState(true)
  const [showYamlPanel, setShowYamlPanel] = useState(false)
  const [showLayersPanel, setShowLayersPanel] = useState(false)
  const [showDocPanel, setShowDocPanel] = useState(false)
  const [pageElement, setPageElement] = useState<HTMLElement | null>(null)
  const layersBtnRef = useRef<HTMLButtonElement | null>(null)
  const docBtnRef = useRef<HTMLButtonElement | null>(null)
  const topRulerRef = useRef<HTMLDivElement | null>(null)
  const leftRulerRef = useRef<HTMLDivElement | null>(null)
  const stageRef = useRef<HTMLDivElement | null>(null)
  const fileInputRef = useRef<HTMLInputElement | null>(null)
  const moveableRef = useRef<Moveable | null>(null)
  const horizontalGuidesRef = useRef<GuidesInstance | null>(null)
  const verticalGuidesRef = useRef<GuidesInstance | null>(null)

  const schemaObject = useMemo(() => buildSchema(config), [config])
  const yamlOutput = useMemo(() => toYaml(schemaObject), [schemaObject])
  const selectedId = selectedIds.at(-1) ?? null
  const selectedBlocks = useMemo(
    () => config.blocks.filter((block) => selectedIds.includes(block.id)),
    [config.blocks, selectedIds],
  )
  const selectedTargetSelectors = useMemo(() => selectedIds.map(getBlockSelector), [selectedIds])
  const elementGuidelines = useMemo(
    () => config.blocks.map((block) => getBlockSelector(block.id)),
    [config.blocks],
  )
  const isGroupSelection = selectedIds.length > 1
  const selectionType =
    selectedBlocks.length > 0 && selectedBlocks.every((block) => block.type === selectedBlocks[0].type)
      ? selectedBlocks[0].type
      : null
  const selectedGroupIds = [...new Set(selectedBlocks.map((block) => block.groupId).filter((value): value is string => Boolean(value)))]
  const selectionBounds = getSelectionBounds(selectedBlocks)

  const selectedBlock = config.blocks.find((b) => b.id === selectedId) ?? null

  function syncGuidesScroll(nextZoom = canvasZoom) {
    const stage = stageRef.current
    if (!stage) {
      return
    }

    horizontalGuidesRef.current?.scroll(stage.scrollLeft, nextZoom)
    verticalGuidesRef.current?.scroll(stage.scrollTop, nextZoom)
  }

  useEffect(() => {
    const topRulerHost = topRulerRef.current
    const leftRulerHost = leftRulerRef.current

    if (!topRulerHost || !leftRulerHost) {
      return
    }

    topRulerHost.replaceChildren()
    leftRulerHost.replaceChildren()

    const horizontalGuides = new Guides(topRulerHost, {
      type: 'horizontal',
      unit: 50,
      zoom: 1,
      displayGuidePos: true,
      lockGuides: true,
    })
    const verticalGuides = new Guides(leftRulerHost, {
      type: 'vertical',
      unit: 50,
      zoom: 1,
      displayGuidePos: true,
      lockGuides: true,
    })

    horizontalGuides.loadGuides(CANVAS_VERTICAL_GUIDES)
    verticalGuides.loadGuides(CANVAS_HORIZONTAL_GUIDES)
    horizontalGuides.resize()
    verticalGuides.resize()
    horizontalGuidesRef.current = horizontalGuides as GuidesInstance
    verticalGuidesRef.current = verticalGuides as GuidesInstance
    syncGuidesScroll(1)

    const syncAfterPaint = requestAnimationFrame(() => {
      horizontalGuides.resize()
      verticalGuides.resize()
      syncGuidesScroll(canvasZoom)
    })

    const handleResize = () => {
      horizontalGuides.resize()
      verticalGuides.resize()
      syncGuidesScroll(canvasZoom)
    }

    window.addEventListener('resize', handleResize)

    return () => {
      cancelAnimationFrame(syncAfterPaint)
      window.removeEventListener('resize', handleResize)
      horizontalGuidesRef.current = null
      verticalGuidesRef.current = null
      horizontalGuides.destroy()
      verticalGuides.destroy()
      topRulerHost.replaceChildren()
      leftRulerHost.replaceChildren()
    }
  }, [])

  useEffect(() => {
    horizontalGuidesRef.current?.zoomTo(canvasZoom)
    verticalGuidesRef.current?.zoomTo(canvasZoom)
    horizontalGuidesRef.current?.resize(canvasZoom)
    verticalGuidesRef.current?.resize(canvasZoom)
    syncGuidesScroll(canvasZoom)
  }, [canvasZoom])

  useEffect(() => {
    if (!showRulers || !pageElement) {
      return
    }

    const syncAfterPaint = requestAnimationFrame(() => {
      horizontalGuidesRef.current?.resize(canvasZoom)
      verticalGuidesRef.current?.resize(canvasZoom)
      syncGuidesScroll(canvasZoom)
    })

    return () => {
      cancelAnimationFrame(syncAfterPaint)
    }
  }, [canvasZoom, pageElement, showRulers])

  useEffect(() => {
    if (!isInspectorOpen && !showLayersPanel && !showDocPanel) {
      return
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsInspectorOpen(false)
        setShowLayersPanel(false)
        setShowDocPanel(false)
      }
    }

    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as Node
      if (
        layersBtnRef.current && !layersBtnRef.current.contains(target) &&
        docBtnRef.current && !docBtnRef.current.contains(target) &&
        !(target as Element).closest?.('.toolbar-dropdown')
      ) {
        setShowLayersPanel(false)
        setShowDocPanel(false)
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    window.addEventListener('mousedown', handleClickOutside)

    return () => {
      window.removeEventListener('keydown', handleKeyDown)
      window.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isInspectorOpen, showLayersPanel, showDocPanel])

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (isTextEntryTarget(event.target)) {
        return
      }

      const isMeta = event.metaKey || event.ctrlKey
      const key = event.key.toLowerCase()

      if ((event.key === 'Delete' || event.key === 'Backspace') && selectedIds.length > 0) {
        event.preventDefault()
        removeSelectedBlock()
        return
      }

      if (isMeta && !event.shiftKey && key === 'd' && selectedIds.length > 0) {
        event.preventDefault()
        duplicateSelected()
        return
      }

      if (isMeta && !event.shiftKey && key === 'g' && selectedIds.length > 1) {
        event.preventDefault()
        groupSelected()
        return
      }

      if (isMeta && event.shiftKey && key === 'g' && selectedGroupIds.length > 0) {
        event.preventDefault()
        ungroupSelected()
      }
    }

    window.addEventListener('keydown', handleKeyDown)

    return () => {
      window.removeEventListener('keydown', handleKeyDown)
    }
  }, [selectedIds, selectedGroupIds])

  function setSelection(ids: string[], primaryId?: string | null) {
    setSelectedIds(expandSelectionForBlocks(config.blocks, ids, primaryId))
  }

  function setPrimarySelection(id: string | null) {
    setSelection(id ? [id] : [], id)
  }

  function updateBlocks(blockIds: string[], updater: (current: Block) => Block) {
    if (blockIds.length === 0) {
      return
    }

    const ids = new Set(blockIds)

    setConfig((prev) => ({
      ...prev,
      blocks: prev.blocks.map((block) => (ids.has(block.id) ? updater(block) : block)),
    }))
  }

  function updateBlock(blockId: string, updater: (current: Block) => Block) {
    updateBlocks([blockId], updater)
  }

  function addBlock(type: BlockType) {
    const block = createBlock(type, config.blocks.length)

    setConfig((prev) => ({ ...prev, blocks: [...prev.blocks, block] }))
    setSelectedIds([block.id])
  }

  function removeSelectedBlock() {
    if (selectedIds.length === 0) {
      return
    }

    setConfig((prev) => ({
      ...prev,
      blocks: prev.blocks.filter((block) => !selectedIds.includes(block.id)),
    }))
    setSelectedIds([])
  }

  function updateSelectedBlock(updater: (current: Block) => Block) {
    if (!selectedId) {
      return
    }

    updateBlock(selectedId, updater)
  }

  function updateSelectedBlocks(updater: (current: Block) => Block) {
    updateBlocks(selectedIds, updater)
  }

  function updateBlockFrame(blockId: string, patch: Partial<BlockFrame>) {
    updateBlock(blockId, (block) => ({
      ...block,
      frame: clampFrame({ ...block.frame, ...patch }, block.type),
    }))
  }

  function updateSelectedFrame(patch: Partial<BlockFrame>) {
    if (!selectedId) {
      return
    }

    updateBlockFrame(selectedId, patch)
  }

  function moveSelected(offset: -1 | 1) {
    if (!selectedId) {
      return
    }

    setConfig((prev) => {
      const index = prev.blocks.findIndex((block) => block.id === selectedId)
      if (index < 0) {
        return prev
      }

      const target = index + offset
      if (target < 0 || target >= prev.blocks.length) {
        return prev
      }

      const copy = [...prev.blocks]
      const [item] = copy.splice(index, 1)
      copy.splice(target, 0, item)
      return { ...prev, blocks: copy }
    })
  }

  function duplicateSelected() {
    if (selectedIds.length === 0) {
      return
    }

    const nextSelection: string[] = []

    setConfig((prev) => {
      const ids = new Set(selectedIds)
      const groupMap = new Map<string, string>()
      const clones = prev.blocks
        .filter((block) => ids.has(block.id))
        .map((block) => {
          const nextGroup = block.groupId
            ? (groupMap.get(block.groupId) ?? (() => {
                const created = nextGroupId()
                groupMap.set(block.groupId, created)
                return created
              })())
            : undefined
          const clone = {
            ...block,
            id: nextId(),
            groupId: nextGroup,
            frame: clampFrame(
              {
                ...block.frame,
                x: block.frame.x + 24,
                y: block.frame.y + 24,
              },
              block.type,
            ),
          } as Block

          nextSelection.push(clone.id)
          return clone
        })

      return { ...prev, blocks: [...prev.blocks, ...clones] }
    })

    setSelectedIds(nextSelection)
  }

  function groupSelected() {
    if (selectedIds.length < 2) {
      return
    }

    const groupId = nextGroupId()

    updateSelectedBlocks((block) => ({ ...block, groupId }))
  }

  function ungroupSelected() {
    if (selectedIds.length === 0) {
      return
    }

    updateSelectedBlocks((block) => ({ ...block, groupId: undefined }))
  }

  function alignSelection(mode: 'left' | 'center' | 'right' | 'top' | 'middle' | 'bottom') {
    if (!selectionBounds || selectedIds.length === 0) {
      return
    }

    updateSelectedBlocks((block) => {
      const patch: Partial<BlockFrame> = {}

      if (mode === 'left') {
        patch.x = selectionBounds.x
      }

      if (mode === 'center') {
        patch.x = selectionBounds.x + selectionBounds.width / 2 - block.frame.width / 2
      }

      if (mode === 'right') {
        patch.x = selectionBounds.x + selectionBounds.width - block.frame.width
      }

      if (mode === 'top') {
        patch.y = selectionBounds.y
      }

      if (mode === 'middle') {
        patch.y = selectionBounds.y + selectionBounds.height / 2 - block.frame.height / 2
      }

      if (mode === 'bottom') {
        patch.y = selectionBounds.y + selectionBounds.height - block.frame.height
      }

      return {
        ...block,
        frame: clampFrame({ ...block.frame, ...patch }, block.type),
      }
    })
  }

  function alignSelectionToPage(mode: 'left' | 'center' | 'right') {
    if (selectedIds.length === 0) {
      return
    }

    const frame = {
      x: PAGE_MARGIN,
      y: PAGE_HEADER_Y,
      width: PAGE_WIDTH - PAGE_MARGIN * 2,
      height: PAGE_FOOTER_Y - PAGE_HEADER_Y,
    }

    updateSelectedBlocks((block) => {
      const x =
        mode === 'left'
          ? frame.x
          : mode === 'center'
            ? frame.x + frame.width / 2 - block.frame.width / 2
            : frame.x + frame.width - block.frame.width

      return {
        ...block,
        frame: clampFrame({ ...block.frame, x }, block.type),
      }
    })
  }

  function distributeSelection(axis: 'horizontal' | 'vertical') {
    if (selectedBlocks.length < 3) {
      return
    }

    const sorted = [...selectedBlocks].sort((left, right) =>
      axis === 'horizontal' ? left.frame.x - right.frame.x : left.frame.y - right.frame.y,
    )
    const start = axis === 'horizontal' ? sorted[0].frame.x : sorted[0].frame.y
    const end =
      axis === 'horizontal'
        ? sorted.at(-1)!.frame.x + sorted.at(-1)!.frame.width
        : sorted.at(-1)!.frame.y + sorted.at(-1)!.frame.height
    const totalSize = sorted.reduce(
      (total, block) => total + (axis === 'horizontal' ? block.frame.width : block.frame.height),
      0,
    )
    const gap = (end - start - totalSize) / (sorted.length - 1)
    let cursor = start
    const positions = new Map<string, number>()

    sorted.forEach((block) => {
      positions.set(block.id, cursor)
      cursor += (axis === 'horizontal' ? block.frame.width : block.frame.height) + gap
    })

    updateSelectedBlocks((block) => {
      const position = positions.get(block.id)

      if (position === undefined) {
        return block
      }

      return {
        ...block,
        frame: clampFrame(
          {
            ...block.frame,
            ...(axis === 'horizontal' ? { x: position } : { y: position }),
          },
          block.type,
        ),
      }
    })
  }

  function matchSelectionSize(dimension: 'width' | 'height') {
    if (!selectedBlock || selectedBlocks.length < 2) {
      return
    }

    updateSelectedBlocks((block) => {
      if (block.id === selectedBlock.id) {
        return block
      }

      return {
        ...block,
        frame: clampFrame(
          {
            ...block.frame,
            [dimension]: selectedBlock.frame[dimension],
          },
          block.type,
        ),
      }
    })
  }

  function requestImageUpload() {
    fileInputRef.current?.click()
  }

  function loadImageFile(file: File | null) {
    if (!file || selectionType !== 'image') {
      return
    }

    const reader = new FileReader()

    reader.onload = () => {
      const result = typeof reader.result === 'string' ? reader.result : ''
      updateSelectedBlocks((block) =>
        block.type === 'image'
          ? {
              ...block,
              source: result,
              sourceMode: 'base64',
              alt: block.alt || file.name,
            }
          : block,
      )
    }

    reader.readAsDataURL(file)
  }

  function downloadYaml() {
    const blob = new Blob([yamlOutput], { type: 'application/x-yaml;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${config.name || 'report'}.frpt.yaml`
    a.click()
    URL.revokeObjectURL(url)
  }

  async function copyYaml() {
    await navigator.clipboard.writeText(yamlOutput)
  }

  function openInspector(blockId?: string) {
    if (blockId) {
      setPrimarySelection(blockId)
    }

    setIsInspectorOpen(true)
  }

  function closeInspector() {
    setIsInspectorOpen(false)
  }

  function stepZoom(direction: -1 | 1) {
    setCanvasZoom((currentZoom) => getNextZoom(currentZoom, direction))
  }

  function resetZoom() {
    setCanvasZoom(1)
  }

  function handleStageScroll() {
    syncGuidesScroll(canvasZoom)
  }

  function handleDrag(event: {
    target: HTMLElement | SVGElement
    beforeTranslate: number[]
  }) {
    const blockId = event.target.dataset.blockId
    if (!blockId) {
      return
    }

    const [x, y] = event.beforeTranslate

    if (!(event.target instanceof HTMLElement)) {
      return
    }

    event.target.style.transform = `translate(${x}px, ${y}px)`
    updateBlockFrame(blockId, { x, y })
  }

  function handleResize(event: {
    target: HTMLElement | SVGElement
    width: number
    height: number
    drag: { beforeTranslate: number[] }
  }) {
    const blockId = event.target.dataset.blockId
    if (!blockId) {
      return
    }

    const [x, y] = event.drag.beforeTranslate

    if (!(event.target instanceof HTMLElement)) {
      return
    }

    event.target.style.width = `${event.width}px`
    event.target.style.height = `${event.height}px`
    event.target.style.transform = `translate(${x}px, ${y}px)`

    updateBlockFrame(blockId, {
      x,
      y,
      width: event.width,
      height: event.height,
    })
  }

  function renderBlockBody(block: Block) {
    if (block.type === 'text') {
      return (
        <>
          <div className="canvas-block-chip">Text</div>
          <p
            style={{
              textAlign: block.align ?? 'left',
              fontSize: block.fontSize ? `${block.fontSize}px` : undefined,
              fontWeight: block.bold ? 700 : undefined,
              fontStyle: block.italic ? 'italic' : undefined,
              color: block.color ?? undefined,
            }}
          >
            {block.value}
          </p>
        </>
      )
    }

    if (block.type === 'line') {
      return (
        <>
          <div className="canvas-block-chip">Line</div>
          <div
            className="canvas-line"
            style={{
              borderTopWidth: `${block.thickness}px`,
              borderTopColor: block.color,
            }}
          />
        </>
      )
    }

    if (block.type === 'spacer') {
      return (
        <>
          <div className="canvas-block-chip">Spacer</div>
          <div className="canvas-spacer">Spacer {block.size}px</div>
        </>
      )
    }

    if (block.type === 'image') {
      return (
        <>
          <div className="canvas-block-chip">Image</div>
          {block.source ? (
            <img
              className="canvas-image"
              src={block.source}
              alt={block.alt ?? 'Selected image'}
              style={{ objectFit: block.fit === 'none' ? 'fill' : block.fit }}
            />
          ) : (
            <div className="canvas-image-placeholder">Image placeholder</div>
          )}
        </>
      )
    }

    return (
      <>
        <div className="canvas-block-chip">Page Break</div>
        <div className="canvas-page-break">Page Break</div>
      </>
    )
  }

  function renderInspectorContent() {
    if (!selectedBlock) {
      return (
        <div className="empty-state">
          <strong>Nada seleccionado</strong>
        </div>
      )
    }

    return (
      <>
        <section className="panel-section">
          <div className="section-heading">
            <div>
              <h3>Frame</h3>
            </div>
            <span className="section-chip mono">px</span>
          </div>
          <div className="frame-grid">
            <label>
              <span className="field-label">X</span>
              <input
                type="number"
                value={Math.round(selectedBlock.frame.x)}
                onChange={(e) => updateSelectedFrame({ x: Number(e.target.value) || 0 })}
              />
            </label>
            <label>
              <span className="field-label">Y</span>
              <input
                type="number"
                value={Math.round(selectedBlock.frame.y)}
                onChange={(e) => updateSelectedFrame({ y: Number(e.target.value) || 0 })}
              />
            </label>
            <label>
              <span className="field-label">Width</span>
              <input
                type="number"
                value={Math.round(selectedBlock.frame.width)}
                onChange={(e) => updateSelectedFrame({ width: Number(e.target.value) || 0 })}
              />
            </label>
            <label>
              <span className="field-label">Height</span>
              <input
                type="number"
                value={Math.round(selectedBlock.frame.height)}
                onChange={(e) => updateSelectedFrame({ height: Number(e.target.value) || 0 })}
              />
            </label>
          </div>
        </section>

        {selectedBlock.type === 'text' && (
          <>
          <section className="panel-section">
            <div className="section-heading">
              <div>
                <h3>Contenido</h3>
              </div>
            </div>
            <div className="field-stack">
              <label>
                <span className="field-label">Texto</span>
                <textarea
                  rows={5}
                  value={selectedBlock.value}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'text' ? { ...block, value: e.target.value } : block,
                    )
                  }
                />
              </label>
              <label>
                <span className="field-label">styleRef</span>
                <input
                  value={selectedBlock.styleRef ?? ''}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'text'
                        ? {
                            ...block,
                            styleRef: e.target.value || undefined,
                          }
                        : block,
                    )
                  }
                />
              </label>
              <label>
                <span className="field-label">Align</span>
                <select
                  value={selectedBlock.align ?? 'left'}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'text' ? { ...block, align: e.target.value as Align } : block,
                    )
                  }
                >
                  <option value="left">left</option>
                  <option value="center">center</option>
                  <option value="right">right</option>
                  <option value="justify">justify</option>
                </select>
              </label>
            </div>
          </section>

          <section className="panel-section">
            <div className="section-heading">
              <div><h3>Tipografía</h3></div>
            </div>
            <div className="field-stack">
              <div className="field-row">
                <label className="field-grow">
                  <span className="field-label">Tamaño</span>
                  <input
                    type="number"
                    min={6}
                    max={96}
                    placeholder="—"
                    value={selectedBlock.fontSize ?? ''}
                    onChange={(e) =>
                      updateSelectedBlock((block) =>
                        block.type === 'text'
                          ? { ...block, fontSize: e.target.value ? Number(e.target.value) : undefined }
                          : block,
                      )
                    }
                  />
                </label>
                <label className="field-grow">
                  <span className="field-label">Color</span>
                  <div className="color-field">
                    <input
                      type="color"
                      className="color-swatch"
                      value={selectedBlock.color ?? '#162435'}
                      onChange={(e) =>
                        updateSelectedBlock((block) =>
                          block.type === 'text' ? { ...block, color: e.target.value } : block,
                        )
                      }
                    />
                    <input
                      type="text"
                      value={selectedBlock.color ?? ''}
                      placeholder="#162435"
                      onChange={(e) =>
                        updateSelectedBlock((block) =>
                          block.type === 'text'
                            ? { ...block, color: e.target.value || undefined }
                            : block,
                        )
                      }
                    />
                  </div>
                </label>
              </div>
              <div className="field-row">
                <label className="field-toggle">
                  <input
                    type="checkbox"
                    checked={selectedBlock.bold ?? false}
                    onChange={(e) =>
                      updateSelectedBlock((block) =>
                        block.type === 'text' ? { ...block, bold: e.target.checked || undefined } : block,
                      )
                    }
                  />
                  <span>Bold</span>
                </label>
                <label className="field-toggle">
                  <input
                    type="checkbox"
                    checked={selectedBlock.italic ?? false}
                    onChange={(e) =>
                      updateSelectedBlock((block) =>
                        block.type === 'text' ? { ...block, italic: e.target.checked || undefined } : block,
                      )
                    }
                  />
                  <span>Italic</span>
                </label>
              </div>
            </div>
          </section>
          </>
        )}

        {selectedBlock.type === 'line' && (
          <section className="panel-section">
            <div className="section-heading">
              <div>
                <h3>Apariencia</h3>
              </div>
            </div>
            <div className="field-stack">
              <label>
                <span className="field-label">Thickness</span>
                <input
                  type="number"
                  min={1}
                  max={8}
                  value={selectedBlock.thickness}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'line'
                        ? { ...block, thickness: Number(e.target.value) || 1 }
                        : block,
                    )
                  }
                />
              </label>
              <label>
                <span className="field-label">Color</span>
                <input
                  value={selectedBlock.color}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'line' ? { ...block, color: e.target.value } : block,
                    )
                  }
                />
              </label>
            </div>
          </section>
        )}

        {selectedBlock.type === 'spacer' && (
          <section className="panel-section">
            <div className="section-heading">
              <div>
                <h3>Spacing</h3>
              </div>
            </div>
            <div className="field-stack">
              <label>
                <span className="field-label">Size</span>
                <input
                  type="number"
                  min={0}
                  max={100}
                  value={selectedBlock.size}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'spacer' ? { ...block, size: Number(e.target.value) || 0 } : block,
                    )
                  }
                />
              </label>
            </div>
          </section>
        )}

        {selectedBlock.type === 'pageBreak' && (
          <section className="panel-section">
            <div className="empty-state compact">
              <strong>Bloque sin ajustes</strong>
            </div>
          </section>
        )}

        {selectedBlock.type === 'image' && (
          <section className="panel-section">
            <div className="section-heading">
              <div>
                <h3>Imagen</h3>
              </div>
            </div>
            <div className="field-stack">
              <label>
                <span className="field-label">Fuente</span>
                <input
                  value={selectedBlock.source}
                  placeholder="https://... o data:image/..."
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'image'
                        ? {
                            ...block,
                            source: e.target.value,
                            sourceMode: e.target.value.startsWith('data:') ? 'base64' : 'path',
                          }
                        : block,
                    )
                  }
                />
              </label>
              <div className="field-row">
                <label className="field-grow">
                  <span className="field-label">Fit</span>
                  <select
                    value={selectedBlock.fit}
                    onChange={(e) =>
                      updateSelectedBlock((block) =>
                        block.type === 'image' ? { ...block, fit: e.target.value as ImageFit } : block,
                      )
                    }
                  >
                    <option value="contain">contain</option>
                    <option value="cover">cover</option>
                    <option value="none">none</option>
                  </select>
                </label>
                <label className="field-grow">
                  <span className="field-label">Alt</span>
                  <input
                    value={selectedBlock.alt ?? ''}
                    onChange={(e) =>
                      updateSelectedBlock((block) =>
                        block.type === 'image' ? { ...block, alt: e.target.value || undefined } : block,
                      )
                    }
                  />
                </label>
              </div>
              <button type="button" onClick={requestImageUpload}>
                Cargar imagen
              </button>
            </div>
          </section>
        )}

        <section className="panel-section">
          <div className="section-heading">
            <div>
              <h3>Acciones</h3>
            </div>
          </div>
          <div className="property-actions">
            <button type="button" onClick={duplicateSelected}>
              Duplicar
            </button>
            <button type="button" onClick={() => moveSelected(-1)}>
              Subir
            </button>
            <button type="button" onClick={() => moveSelected(1)}>
              Bajar
            </button>
            <button type="button" className="danger" onClick={removeSelectedBlock}>
              Eliminar
            </button>
          </div>
        </section>
      </>
    )
  }

  function renderContextualToolbar() {
    if (selectedIds.length === 0) {
      return (
        <section className="toolbar toolbar-context" aria-label="Context toolbar">
          <div className="toolbar-context-empty">
            Selecciona uno o más elementos para ver acciones contextuales, agruparlos o editar sus propiedades rápidas.
          </div>
        </section>
      )
    }

    const activeTextBlock = selectionType === 'text' && selectedBlock?.type === 'text' ? selectedBlock : null
    const activeLineBlock = selectionType === 'line' && selectedBlock?.type === 'line' ? selectedBlock : null
    const activeSpacerBlock = selectionType === 'spacer' && selectedBlock?.type === 'spacer' ? selectedBlock : null
    const activeImageBlock = selectionType === 'image' && selectedBlock?.type === 'image' ? selectedBlock : null

    return (
      <section className="toolbar toolbar-context" aria-label="Context toolbar">
        <div className="toolbar-group toolbar-context-summary">
          <span className="status-pill accent">
            {selectedIds.length} seleccionado{selectedIds.length > 1 ? 's' : ''}
          </span>
          <span className="toolbar-info">
            {selectionType ? getBlockTypeLabel(selectionType) : 'Selección mixta'}
          </span>
          {selectedGroupIds.length === 1 && <span className="toolbar-info">Grupo {selectedGroupIds[0]}</span>}
          {selectionBounds && (
            <span className="toolbar-info">
              {Math.round(selectionBounds.width)} x {Math.round(selectionBounds.height)}
            </span>
          )}
        </div>

        <div className="toolbar-sep" />

        <div className="toolbar-group">
          <span className="toolbar-label">Selección</span>
          <button type="button" className="toolbar-button" onClick={duplicateSelected} title="Duplicar selección">
            Duplicar
          </button>
          <button
            type="button"
            className="toolbar-button"
            onClick={groupSelected}
            disabled={selectedIds.length < 2}
            title="Agrupar selección"
          >
            Agrupar
          </button>
          <button
            type="button"
            className="toolbar-button"
            onClick={ungroupSelected}
            disabled={selectedGroupIds.length === 0}
            title="Desagrupar selección"
          >
            Desagrupar
          </button>
          <button type="button" className="toolbar-button danger" onClick={removeSelectedBlock} title="Eliminar selección">
            Eliminar
          </button>
        </div>

        <div className="toolbar-sep" />

        <div className="toolbar-group">
          <span className="toolbar-label">Página</span>
          <button type="button" className="toolbar-button" onClick={() => alignSelectionToPage('left')}>
            Margen izq.
          </button>
          <button type="button" className="toolbar-button" onClick={() => alignSelectionToPage('center')}>
            Centrar
          </button>
          <button type="button" className="toolbar-button" onClick={() => alignSelectionToPage('right')}>
            Margen der.
          </button>
        </div>

        {isGroupSelection && (
          <>
            <div className="toolbar-sep" />

            <div className="toolbar-group">
              <span className="toolbar-label">Layout</span>
              <button type="button" className="toolbar-button" onClick={() => alignSelection('left')}>
                Alinear izq.
              </button>
              <button type="button" className="toolbar-button" onClick={() => alignSelection('center')}>
                Centrar X
              </button>
              <button type="button" className="toolbar-button" onClick={() => alignSelection('right')}>
                Alinear der.
              </button>
              <button type="button" className="toolbar-button" onClick={() => alignSelection('top')}>
                Arriba
              </button>
              <button type="button" className="toolbar-button" onClick={() => alignSelection('middle')}>
                Centro Y
              </button>
              <button type="button" className="toolbar-button" onClick={() => alignSelection('bottom')}>
                Abajo
              </button>
              <button type="button" className="toolbar-button" onClick={() => distributeSelection('horizontal')} disabled={selectedBlocks.length < 3}>
                Distribuir X
              </button>
              <button type="button" className="toolbar-button" onClick={() => distributeSelection('vertical')} disabled={selectedBlocks.length < 3}>
                Distribuir Y
              </button>
              <button type="button" className="toolbar-button" onClick={() => matchSelectionSize('width')} disabled={selectedBlocks.length < 2}>
                Igualar ancho
              </button>
              <button type="button" className="toolbar-button" onClick={() => matchSelectionSize('height')} disabled={selectedBlocks.length < 2}>
                Igualar alto
              </button>
            </div>
          </>
        )}

        {selectionType === 'text' && activeTextBlock && (
          <>
            <div className="toolbar-sep" />

            <div className="toolbar-group toolbar-inline-fields">
              <span className="toolbar-label">Texto</span>
              <div className="toolbar-segment" role="group" aria-label="Text align">
                <button type="button" className="toolbar-button" onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, align: 'left' } : block)}>
                  Izq.
                </button>
                <button type="button" className="toolbar-button" onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, align: 'center' } : block)}>
                  Centro
                </button>
                <button type="button" className="toolbar-button" onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, align: 'right' } : block)}>
                  Der.
                </button>
                <button type="button" className="toolbar-button" onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, align: 'justify' } : block)}>
                  Just.
                </button>
              </div>
              <label className="toolbar-field toolbar-field-narrow">
                <span>Size</span>
                <input
                  type="number"
                  min={6}
                  max={96}
                  value={activeTextBlock.fontSize ?? ''}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'text'
                        ? { ...block, fontSize: e.target.value ? Number(e.target.value) : undefined }
                        : block,
                    )
                  }
                />
              </label>
              <label className="toolbar-field toolbar-field-medium">
                <span>StyleRef</span>
                <input
                  value={activeTextBlock.styleRef ?? ''}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'text' ? { ...block, styleRef: e.target.value || undefined } : block,
                    )
                  }
                />
              </label>
              <label className="toolbar-field toolbar-field-color">
                <span>Color</span>
                <input
                  type="color"
                  value={activeTextBlock.color ?? '#162435'}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'text' ? { ...block, color: e.target.value } : block,
                    )
                  }
                />
              </label>
              <button type="button" className={activeTextBlock.bold ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'} onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, bold: !block.bold || undefined } : block)}>
                Bold
              </button>
              <button type="button" className={activeTextBlock.italic ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'} onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, italic: !block.italic || undefined } : block)}>
                Italic
              </button>
            </div>
          </>
        )}

        {selectionType === 'line' && activeLineBlock && (
          <>
            <div className="toolbar-sep" />

            <div className="toolbar-group toolbar-inline-fields">
              <span className="toolbar-label">Línea</span>
              <label className="toolbar-field toolbar-field-narrow">
                <span>Thickness</span>
                <input
                  type="number"
                  min={1}
                  max={8}
                  value={activeLineBlock.thickness}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'line' ? { ...block, thickness: Number(e.target.value) || 1 } : block,
                    )
                  }
                />
              </label>
              <label className="toolbar-field toolbar-field-color">
                <span>Color</span>
                <input
                  type="color"
                  value={activeLineBlock.color}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'line' ? { ...block, color: e.target.value } : block,
                    )
                  }
                />
              </label>
            </div>
          </>
        )}

        {selectionType === 'spacer' && activeSpacerBlock && (
          <>
            <div className="toolbar-sep" />

            <div className="toolbar-group toolbar-inline-fields">
              <span className="toolbar-label">Spacer</span>
              {[4, 8, 12, 16, 24, 32].map((value) => (
                <button
                  type="button"
                  key={value}
                  className="toolbar-button"
                  onClick={() => updateSelectedBlocks((block) => block.type === 'spacer' ? { ...block, size: value } : block)}
                >
                  {value}px
                </button>
              ))}
            </div>
          </>
        )}

        {selectionType === 'image' && activeImageBlock && (
          <>
            <div className="toolbar-sep" />

            <div className="toolbar-group toolbar-inline-fields">
              <span className="toolbar-label">Imagen</span>
              <button type="button" className="toolbar-button" onClick={requestImageUpload}>
                Cargar
              </button>
              <div className="toolbar-segment" role="group" aria-label="Image fit">
                <button type="button" className={activeImageBlock.fit === 'contain' ? 'toolbar-button toolbar-toggle active' : 'toolbar-button'} onClick={() => updateSelectedBlocks((block) => block.type === 'image' ? { ...block, fit: 'contain' } : block)}>
                  Contain
                </button>
                <button type="button" className={activeImageBlock.fit === 'cover' ? 'toolbar-button toolbar-toggle active' : 'toolbar-button'} onClick={() => updateSelectedBlocks((block) => block.type === 'image' ? { ...block, fit: 'cover' } : block)}>
                  Cover
                </button>
                <button type="button" className={activeImageBlock.fit === 'none' ? 'toolbar-button toolbar-toggle active' : 'toolbar-button'} onClick={() => updateSelectedBlocks((block) => block.type === 'image' ? { ...block, fit: 'none' } : block)}>
                  None
                </button>
              </div>
              <label className="toolbar-field toolbar-field-wide">
                <span>Fuente</span>
                <input
                  value={activeImageBlock.source}
                  placeholder="Ruta o URL"
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'image'
                        ? {
                            ...block,
                            source: e.target.value,
                            sourceMode: e.target.value.startsWith('data:') ? 'base64' : 'path',
                          }
                        : block,
                    )
                  }
                />
              </label>
            </div>
          </>
        )}
      </section>
    )
  }

  return (
    <main className={showYamlPanel ? 'editor-app' : 'editor-app yaml-panel-hidden'}>
      <header className="topbar">
        <div className="topbar-copy">
          <h1>FluentReport Schema Studio</h1>
        </div>
        <div className="topbar-meta">
          <span className="status-pill">{config.name || 'untitled-report'}</span>
          <span className="status-pill">{config.blocks.length} bloques</span>
          <span className="status-pill accent">
            {selectedIds.length > 0
              ? `${selectedIds.length} seleccionado${selectedIds.length > 1 ? 's' : ''}`
              : 'Sin seleccion'}
          </span>
        </div>
      </header>

      <section className="toolbar" aria-label="Editor toolbar">
        {/* LEFT: document + layers dropdowns */}
        <div className="toolbar-group">
          <div className="toolbar-dropdown-wrap">
            <button
              ref={docBtnRef}
              type="button"
              className={showDocPanel ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'}
              onClick={() => { setShowDocPanel(v => !v); setShowLayersPanel(false) }}
              title="Documento"
            >
              <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M3 2h7l3 3v9H3V2z" stroke="currentColor" strokeWidth="1.4" strokeLinejoin="round" fill="none"/>
                <path d="M10 2v3h3" stroke="currentColor" strokeWidth="1.4" strokeLinejoin="round"/>
                <path d="M5 7h6M5 9.5h4" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/>
              </svg>
              Documento
              <svg width="10" height="10" viewBox="0 0 10 10" fill="none" xmlns="http://www.w3.org/2000/svg" style={{marginLeft:2}}>
                <path d="M2 3.5l3 3 3-3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            </button>
            {showDocPanel && (
              <div className="toolbar-dropdown">
                <div className="toolbar-dropdown-section">
                  <div className="toolbar-dropdown-label">Metadata</div>
                  <label className="toolbar-dropdown-field">
                    <span>Name</span>
                    <input value={config.name} onChange={(e) => setConfig((prev) => ({ ...prev, name: e.target.value }))} />
                  </label>
                  <label className="toolbar-dropdown-field">
                    <span>Título</span>
                    <input value={config.title} onChange={(e) => setConfig((prev) => ({ ...prev, title: e.target.value }))} />
                  </label>
                  <label className="toolbar-dropdown-field">
                    <span>Param. company</span>
                    <input value={config.companyParam} onChange={(e) => setConfig((prev) => ({ ...prev, companyParam: e.target.value }))} />
                  </label>
                  <label className="toolbar-dropdown-field">
                    <span>Param. period</span>
                    <input value={config.periodParam} onChange={(e) => setConfig((prev) => ({ ...prev, periodParam: e.target.value }))} />
                  </label>
                </div>
              </div>
            )}
          </div>

          <div className="toolbar-dropdown-wrap">
            <button
              ref={layersBtnRef}
              type="button"
              className={showLayersPanel ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'}
              onClick={() => { setShowLayersPanel(v => !v); setShowDocPanel(false) }}
              title="Capas"
            >
              <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M8 2L14 5.5 8 9 2 5.5 8 2z" stroke="currentColor" strokeWidth="1.4" strokeLinejoin="round" fill="none"/>
                <path d="M2 8.5l6 3.5 6-3.5" stroke="currentColor" strokeWidth="1.4" strokeLinejoin="round"/>
              </svg>
              Capas
              <span className="toolbar-badge">{config.blocks.length}</span>
              <svg width="10" height="10" viewBox="0 0 10 10" fill="none" xmlns="http://www.w3.org/2000/svg" style={{marginLeft:2}}>
                <path d="M2 3.5l3 3 3-3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            </button>
            {showLayersPanel && (
              <div className="toolbar-dropdown">
                <div className="toolbar-dropdown-section">
                  <div className="toolbar-dropdown-label">{config.blocks.length} bloques · {selectedIds.length > 0 ? `${selectedIds.length} seleccionado${selectedIds.length > 1 ? 's' : ''}` : 'sin selección'}</div>
                  <div className="block-list">
                    {config.blocks.map((block, index) => (
                      <button
                        type="button"
                        key={block.id}
                        className={selectedId === block.id ? 'block-row selected' : 'block-row'}
                        onClick={() => setPrimarySelection(block.id)}
                      >
                        <span className="block-index">{index + 1}</span>
                        <span className="block-row-copy">
                          <strong>{getBlockTypeLabel(block.type)}</strong>
                          <small>{getBlockSummary(block)}{block.groupId ? ` · ${block.groupId}` : ''}</small>
                        </span>
                        <span className="block-row-frame">
                          {Math.round(block.frame.width)} x {Math.round(block.frame.height)}
                        </span>
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="toolbar-sep" />

        {/* CENTER: insert blocks */}
        <div className="toolbar-group">
          <span className="toolbar-label">Insertar</span>
          <button type="button" className="toolbar-button" onClick={() => addBlock('text')} title="Agregar texto">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M2 4h12M8 4v9" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
            </svg>
            Text
          </button>
          <button type="button" className="toolbar-button" onClick={() => addBlock('line')} title="Agregar línea">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M2 8h12" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
            </svg>
            Line
          </button>
          <button type="button" className="toolbar-button" onClick={() => addBlock('spacer')} title="Agregar espaciador">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M3 5h10M3 11h10M8 5v6" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round"/>
            </svg>
            Spacer
          </button>
          <button type="button" className="toolbar-button" onClick={() => addBlock('pageBreak')} title="Agregar salto de página">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M2 8h12" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeDasharray="2 2"/>
              <path d="M5 5l-2 3 2 3M11 5l2 3-2 3" stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
            Break
          </button>
          <button type="button" className="toolbar-button" onClick={() => addBlock('image')} title="Agregar imagen">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="2" y="3" width="12" height="10" rx="1.5" stroke="currentColor" strokeWidth="1.3" />
              <circle cx="6" cy="6.5" r="1" fill="currentColor" />
              <path d="M4 11l2.5-2.5 2 2L10.5 8l1.5 3" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
            Image
          </button>
        </div>

        <div className="toolbar-sep" />

        {/* ZOOM */}
        <div className="toolbar-group">
          <div className="toolbar-segment" role="group" aria-label="Zoom">
            <button type="button" className="toolbar-button toolbar-icon-button" onClick={() => stepZoom(-1)} title="Reducir zoom">
              <svg width="12" height="12" viewBox="0 0 12 12" fill="none"><path d="M2 6h8" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round"/></svg>
            </button>
            <button type="button" className="toolbar-button toolbar-value-button" onClick={resetZoom} title="Restablecer zoom">
              {Math.round(canvasZoom * 100)}%
            </button>
            <button type="button" className="toolbar-button toolbar-icon-button" onClick={() => stepZoom(1)} title="Aumentar zoom">
              <svg width="12" height="12" viewBox="0 0 12 12" fill="none"><path d="M6 2v8M2 6h8" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round"/></svg>
            </button>
          </div>
          <button
            type="button"
            className={showRulers ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'}
            onClick={() => setShowRulers((v) => !v)}
            title="Reglas"
          >
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="1" y="5" width="14" height="6" rx="1" stroke="currentColor" strokeWidth="1.3" fill="none"/>
              <path d="M4 5v2M7 5v3M10 5v2M13 5v2" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/>
            </svg>
            Reglas
          </button>
        </div>

        <div className="toolbar-spacer" />

        {/* RIGHT: actions */}
        <div className="toolbar-group">
          <button
            type="button"
            className={showYamlPanel ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'}
            onClick={() => setShowYamlPanel((value) => !value)}
            title={showYamlPanel ? 'Ocultar YAML' : 'Mostrar YAML'}
          >
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M3 4h10M3 8h10M3 12h10" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round"/>
            </svg>
            YAML
          </button>
          <button
            type="button"
            className="toolbar-button"
            onClick={() => openInspector()}
            disabled={!selectedBlock}
            title="Abrir inspector"
          >
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="8" cy="8" r="5.5" stroke="currentColor" strokeWidth="1.4"/>
              <path d="M8 7v4M8 5.5v.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
            </svg>
            Inspector
          </button>
          <button type="button" className="toolbar-button" onClick={copyYaml} title="Copiar YAML al portapapeles">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="5" y="1" width="8" height="10" rx="1.5" stroke="currentColor" strokeWidth="1.3" fill="none"/>
              <rect x="2" y="4" width="8" height="10" rx="1.5" stroke="currentColor" strokeWidth="1.3" fill="none" style={{fill: 'var(--bg-a)'}}/>
            </svg>
            Copiar YAML
          </button>
          <button type="button" className="toolbar-button primary" onClick={downloadYaml} title="Descargar archivo .frpt.yaml">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M8 2v8M5 8l3 3 3-3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
              <path d="M2 13h12" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round"/>
            </svg>
            Descargar
          </button>
        </div>
      </section>

      {renderContextualToolbar()}

      <section className="layout">
        <section className="center-column">
          <section className="panel preview-panel">
            <div
              className={showRulers ? 'designer-stage' : 'designer-stage rulers-hidden'}
            >
              <div
                className={showRulers ? 'designer-ruler designer-ruler-top' : 'designer-ruler designer-ruler-top is-hidden'}
                ref={topRulerRef}
              />
              <div
                className={showRulers ? 'designer-ruler designer-ruler-left' : 'designer-ruler designer-ruler-left is-hidden'}
                ref={leftRulerRef}
              />

              <div className="designer-scroll-region" ref={stageRef} onScroll={handleStageScroll}>
                <div className="designer-canvas-shell">
                  <div
                    className="paper-canvas-viewport"
                    style={{
                      width: `${PAGE_WIDTH * canvasZoom}px`,
                      height: `${PAGE_HEIGHT * canvasZoom}px`,
                    }}
                  >
                    <article
                      ref={setPageElement}
                      className="paper-canvas"
                      style={{ transform: `scale(${canvasZoom})`, transformOrigin: 'top left' }}
                      onMouseDown={(event) => {
                        if (event.target === event.currentTarget) {
                          setPrimarySelection(null)
                        }
                      }}
                    >
                      {CANVAS_VERTICAL_GUIDES.map((guide) => (
                        <div
                          key={`v-${guide}`}
                          className="canvas-guide canvas-guide-vertical"
                          style={{ left: `${guide}px` }}
                        />
                      ))}
                      {CANVAS_HORIZONTAL_GUIDES.map((guide) => (
                        <div
                          key={`h-${guide}`}
                          className="canvas-guide canvas-guide-horizontal"
                          style={{ top: `${guide}px` }}
                        />
                      ))}

                      {config.blocks.map((block, index) => (
                        <div
                          key={block.id}
                          data-block-id={block.id}
                          className={[
                            'canvas-block',
                            `is-${block.type}`,
                            block.groupId ? 'has-group' : '',
                            selectedIds.includes(block.id) ? 'selected' : '',
                          ]
                            .filter(Boolean)
                            .join(' ')}
                          style={{
                            width: `${block.frame.width}px`,
                            height: `${block.frame.height}px`,
                            transform: `translate(${block.frame.x}px, ${block.frame.y}px)`,
                            zIndex: index + 1,
                          }}
                          onMouseDown={() => {
                            setPrimarySelection(block.id)
                          }}
                          onDoubleClick={() => openInspector(block.id)}
                        >
                          {renderBlockBody(block)}
                          {selectedIds.includes(block.id) && (
                            <div className="canvas-block-dimensions">
                              {Math.round(block.frame.x)}, {Math.round(block.frame.y)} •{' '}
                              {Math.round(block.frame.width)} x {Math.round(block.frame.height)}
                            </div>
                          )}
                        </div>
                      ))}

                      <Selecto
                        dragContainer={pageElement ?? undefined}
                        selectableTargets={['.canvas-block']}
                        selectByClick
                        selectFromInside={false}
                        continueSelect={false}
                        hitRate={0}
                        onDragStart={(event: { inputEvent: MouseEvent; stop: () => void }) => {
                          const inputTarget = event.inputEvent.target

                          if (!(inputTarget instanceof Element)) {
                            return
                          }

                          if (
                            moveableRef.current?.isMoveableElement(inputTarget) ||
                            inputTarget.closest('.canvas-block')
                          ) {
                            event.stop()
                          }
                        }}
                        onSelectEnd={(
                          event: {
                            selected?: Array<HTMLElement | SVGElement>
                            selectedAfterSelect?: Array<HTMLElement | SVGElement>
                          },
                        ) => {
                          const selectedElements = event.selectedAfterSelect ?? event.selected ?? []
                          const nextSelection = selectedElements
                            .filter((element): element is HTMLElement => element instanceof HTMLElement)
                            .map((element) => element.dataset.blockId)
                            .filter((value): value is string => Boolean(value))

                          setSelectedIds(expandSelectionForBlocks(config.blocks, nextSelection, nextSelection.at(-1) ?? null))
                        }}
                      />

                      <Moveable
                        ref={moveableRef}
                        target={
                          isGroupSelection
                            ? selectedTargetSelectors
                            : selectedTargetSelectors[0] ?? null
                        }
                        container={pageElement}
                        origin={false}
                        draggable
                        resizable
                        snappable
                        groupable={isGroupSelection}
                        keepRatio={false}
                        zoom={canvasZoom}
                        bounds={{ left: 0, top: 0, right: PAGE_WIDTH, bottom: PAGE_HEIGHT }}
                        verticalGuidelines={CANVAS_VERTICAL_GUIDES}
                        horizontalGuidelines={CANVAS_HORIZONTAL_GUIDES}
                        elementGuidelines={elementGuidelines}
                        snapGap
                        onDrag={(event: { target: HTMLElement | SVGElement; beforeTranslate: number[] }) => {
                          handleDrag(event)
                        }}
                        onDragGroup={(
                          event: { events: Array<{ target: HTMLElement | SVGElement; beforeTranslate: number[] }> },
                        ) => {
                          event.events.forEach(handleDrag)
                        }}
                        onResize={(event: {
                          target: HTMLElement | SVGElement
                          width: number
                          height: number
                          drag: { beforeTranslate: number[] }
                        }) => {
                          handleResize(event)
                        }}
                        onResizeGroup={(
                          event: {
                            events: Array<{
                              target: HTMLElement | SVGElement
                              width: number
                              height: number
                              drag: { beforeTranslate: number[] }
                            }>
                          },
                        ) => {
                          event.events.forEach(handleResize)
                        }}
                      />
                    </article>
                  </div>
                </div>
              </div>
            </div>
          </section>
        </section>
      </section>

      {showYamlPanel && (
        <section className="panel output-panel">
          <textarea className="yaml-output" value={yamlOutput} readOnly />
        </section>
      )}

      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        className="visually-hidden"
        onChange={(event) => {
          loadImageFile(event.target.files?.[0] ?? null)
          event.target.value = ''
        }}
      />

      {isInspectorOpen && (
        <div className="modal-backdrop" onClick={closeInspector}>
          <section className="panel modal-panel inspector-panel" onClick={(event) => event.stopPropagation()}>
            <div className="panel-header modal-header">
              <div>
                <h2>Propiedades</h2>
              </div>
              <div className="modal-actions">
                <span className={selectedBlock ? 'panel-badge accent' : 'panel-badge'}>
                  {selectedBlock ? getBlockTypeLabel(selectedBlock.type) : 'Sin seleccion'}
                </span>
                <button type="button" className="toolbar-button" onClick={closeInspector}>
                  Cerrar
                </button>
              </div>
            </div>
            {renderInspectorContent()}
          </section>
        </div>
      )}
    </main>
  )
}

export default App
