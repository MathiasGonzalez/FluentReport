import Guides from '@scena/guides'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Moveable from 'react-moveable'
import Selecto from 'react-selecto'
import './App.css'
import { TooltipButton } from './components/TooltipButton'
import {
  EDITOR_COPY,
  EDITOR_LANGUAGE_STORAGE_KEY,
  LOCALE_OPTIONS,
  getInitialLocale,
  isLocale,
  type Locale,
} from './editorCopy'
import {
  DEFAULT_HTML_RENDERER_OPTIONS,
  PAGE_FOOTER_Y,
  PAGE_HEADER_Y,
  PAGE_HEIGHT,
  PAGE_MARGIN,
  PAGE_WIDTH,
  getSelectionBounds,
  type Align,
  type Block,
  type BlockFrame,
  type BlockType,
  type ImageFit,
  type PageDefinition,
  type ReportConfig,
} from './reportModel'
import {
  CANVAS_HORIZONTAL_GUIDES,
  CANVAS_VERTICAL_GUIDES,
  clampFrame,
  createBlock,
  expandSelectionForBlocks,
  formatNameList,
  formatTableColumns,
  getBlockSelector,
  getBlockSummary,
  getBlockTypeLabel,
  getNextZoom,
  initialConfig,
  isExactGroupSelection,
  isTextEntryTarget,
  nextBlockId,
  nextGroupId,
  nextPageId,
  parseNameList,
  parseTableColumns,
} from './editor/editorHelpers'
import {
  clearDraftConfig,
  createProjectFileContent,
  getDraftSavedAt,
  loadDraftConfig,
  parseProjectFileContent,
  saveDraftConfig,
} from './editor/projectStorage'
import { buildSchema, toYaml } from './reportSchema'

type GuidesInstance = Guides & {
  scroll: (pos: number, nextZoom?: number) => void
  zoomTo: (nextZoom: number, nextGuidesZoom?: number) => void
  resize: (nextZoom?: number) => void
}
function App() {
  const bootConfig = useMemo(() => loadDraftConfig() ?? initialConfig, [])

  const [config, setConfig] = useState<ReportConfig>(bootConfig)
  const [locale, setLocale] = useState<Locale>(getInitialLocale)
  const [activePageId, setActivePageId] = useState<string>(bootConfig.pages[0]?.id ?? 'p1')
  const [selectedIds, setSelectedIds] = useState<string[]>(() => {
    const initialPageId = bootConfig.pages[0]?.id ?? 'p1'
    const firstBlock = bootConfig.blocks.find((block) => block.pageId === initialPageId)
    return firstBlock ? [firstBlock.id] : []
  })
  const [canvasZoom, setCanvasZoom] = useState(1)
  const [isInspectorOpen, setIsInspectorOpen] = useState(false)
  const [showRulers, setShowRulers] = useState(true)
  const [showYamlPanel, setShowYamlPanel] = useState(false)
  const [showLayersPanel, setShowLayersPanel] = useState(false)
  const [showDocPanel, setShowDocPanel] = useState(false)
  const [lastSavedAt, setLastSavedAt] = useState<string | null>(getDraftSavedAt)
  const [moveTargetPageId, setMoveTargetPageId] = useState<string>('')
  const [pageElement, setPageElement] = useState<HTMLElement | null>(null)
  const [canvasViewportElement, setCanvasViewportElement] = useState<HTMLDivElement | null>(null)
  const canvasZoomRef = useRef(1)
  const layersBtnRef = useRef<HTMLButtonElement | null>(null)
  const docBtnRef = useRef<HTMLButtonElement | null>(null)
  const topRulerRef = useRef<HTMLDivElement | null>(null)
  const leftRulerRef = useRef<HTMLDivElement | null>(null)
  const stageRef = useRef<HTMLDivElement | null>(null)
  const fileInputRef = useRef<HTMLInputElement | null>(null)
  const projectFileInputRef = useRef<HTMLInputElement | null>(null)
  const selectedIdsRef = useRef<string[]>([])
  const selectedGroupIdsRef = useRef<string[]>([])
  const removeSelectedBlockRef = useRef<() => void>(() => {})
  const duplicateSelectedRef = useRef<() => void>(() => {})
  const groupSelectedRef = useRef<() => void>(() => {})
  const ungroupSelectedRef = useRef<() => void>(() => {})
  const moveableRef = useRef<Moveable | null>(null)
  const horizontalGuidesRef = useRef<GuidesInstance | null>(null)
  const verticalGuidesRef = useRef<GuidesInstance | null>(null)
  const copy = EDITOR_COPY[locale]

  useEffect(() => {
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(EDITOR_LANGUAGE_STORAGE_KEY, locale)
    }
  }, [locale])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      const savedAt = saveDraftConfig(config)
      if (savedAt) {
        setLastSavedAt(savedAt)
      }
    }, 450)

    return () => {
      window.clearTimeout(timeoutId)
    }
  }, [config])

  const schemaObject = useMemo(() => buildSchema(config), [config])
  const yamlOutput = useMemo(() => toYaml(schemaObject), [schemaObject])
  const activePageIndex = Math.max(config.pages.findIndex((page) => page.id === activePageId), 0)
  const activePage = config.pages[activePageIndex] ?? config.pages[0] ?? null
  const currentPageBlocks = useMemo(
    () => config.blocks.filter((block) => block.pageId === activePage?.id),
    [activePage, config.blocks],
  )
  const selectedId = selectedIds.at(-1) ?? null
  const selectedBlocks = useMemo(
    () => currentPageBlocks.filter((block) => selectedIds.includes(block.id)),
    [currentPageBlocks, selectedIds],
  )
  const selectedTargetSelectors = useMemo(() => selectedIds.map(getBlockSelector), [selectedIds])
  const elementGuidelines = useMemo(
    () => currentPageBlocks.map((block) => getBlockSelector(block.id)),
    [currentPageBlocks],
  )
  const isGroupSelection = selectedIds.length > 1
  const selectionType =
    selectedBlocks.length > 0 && selectedBlocks.every((block) => block.type === selectedBlocks[0].type)
      ? selectedBlocks[0].type
      : null
  const selectedGroupIds = useMemo(
    () => [...new Set(selectedBlocks.map((block) => block.groupId).filter((value): value is string => Boolean(value)))],
    [selectedBlocks],
  )
  const selectedWholeGroupId =
    selectedGroupIds.length === 1 && isExactGroupSelection(currentPageBlocks, selectedIds, selectedGroupIds[0])
      ? selectedGroupIds[0]
      : null
  const selectionBounds = getSelectionBounds(selectedBlocks)
  const selectionOutlineInset = selectedWholeGroupId ? 7 : isGroupSelection ? 4 : 0
  const pageMoveOptions = config.pages.filter((page) => page.id !== activePage?.id)
  const fallbackMoveTargetPageId = pageMoveOptions[0]?.id ?? ''
  const resolvedMoveTargetPageId =
    pageMoveOptions.some((page) => page.id === moveTargetPageId) ? moveTargetPageId : fallbackMoveTargetPageId

  const selectedBlock = currentPageBlocks.find((b) => b.id === selectedId) ?? null
  const savedTimeLabel =
    lastSavedAt
      ? copy.lastSaved(
          new Date(lastSavedAt).toLocaleTimeString(locale === 'es' ? 'es-UY' : 'en-US', {
            hour: '2-digit',
            minute: '2-digit',
          }),
        )
      : copy.neverSaved

  useEffect(() => {
    canvasZoomRef.current = canvasZoom
  }, [canvasZoom])

  const syncGuidesScroll = useCallback((nextZoom = canvasZoomRef.current) => {
    const stage = stageRef.current
    if (!stage) {
      return
    }

    horizontalGuidesRef.current?.scroll(stage.scrollLeft, nextZoom)
    verticalGuidesRef.current?.scroll(stage.scrollTop, nextZoom)
  }, [])

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
      syncGuidesScroll(canvasZoomRef.current)
    })

    const handleResize = () => {
      horizontalGuides.resize()
      verticalGuides.resize()
      syncGuidesScroll(canvasZoomRef.current)
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
  }, [syncGuidesScroll])

  useEffect(() => {
    horizontalGuidesRef.current?.zoomTo(canvasZoom)
    verticalGuidesRef.current?.zoomTo(canvasZoom)
    horizontalGuidesRef.current?.resize(canvasZoom)
    verticalGuidesRef.current?.resize(canvasZoom)
    syncGuidesScroll(canvasZoom)
  }, [canvasZoom, syncGuidesScroll])

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
  }, [canvasZoom, pageElement, showRulers, syncGuidesScroll])

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

  function setSelection(ids: string[], primaryId?: string | null) {
    setSelectedIds(expandSelectionForBlocks(currentPageBlocks, ids, primaryId))
  }

  function setPrimarySelection(id: string | null) {
    setSelection(id ? [id] : [], id)
  }

  function setDirectSelection(id: string | null) {
    setSelectedIds(id ? [id] : [])
  }

  function toggleSelection(blockId: string) {
    const block = currentPageBlocks.find((candidate) => candidate.id === blockId)

    if (!block) {
      return
    }

    const relatedIds = block.groupId
      ? currentPageBlocks.filter((candidate) => candidate.groupId === block.groupId).map((candidate) => candidate.id)
      : [blockId]

    setSelectedIds((prev) => {
      const nextIds = new Set(prev)
      const allSelected = relatedIds.every((id) => nextIds.has(id))

      if (allSelected) {
        relatedIds.forEach((id) => nextIds.delete(id))
      } else {
        relatedIds.forEach((id) => nextIds.add(id))
      }

      return expandSelectionForBlocks(currentPageBlocks, [...nextIds], blockId)
    })
  }

  function handleSelectionPointer(blockId: string, modifiers: { ctrlKey: boolean; metaKey: boolean }) {
    const block = currentPageBlocks.find((candidate) => candidate.id === blockId)

    if (!block) {
      return
    }

    if (modifiers.ctrlKey || modifiers.metaKey) {
      toggleSelection(blockId)
      return
    }

    if (selectedIds.includes(blockId)) {
      if (block.groupId && isExactGroupSelection(currentPageBlocks, selectedIds, block.groupId)) {
        setDirectSelection(blockId)
      }

      return
    }

    setPrimarySelection(blockId)
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
    if (!activePage) {
      return
    }

    const block = createBlock(type, currentPageBlocks.length, activePage.id)

    setConfig((prev) => ({ ...prev, blocks: [...prev.blocks, block] }))
    setSelectedIds([block.id])
  }

  function addPage() {
    const page: PageDefinition = { id: nextPageId() }

    setConfig((prev) => ({
      ...prev,
      pages: [...prev.pages, page],
    }))
    setActivePageId(page.id)
    setSelectedIds([])
  }

  function selectPage(pageId: string) {
    setActivePageId(pageId)
    setSelectedIds([])
    setIsInspectorOpen(false)
  }

  function moveActivePage(offset: -1 | 1) {
    if (!activePage) {
      return
    }

    setConfig((prev) => {
      const index = prev.pages.findIndex((page) => page.id === activePage.id)
      const target = index + offset

      if (index < 0 || target < 0 || target >= prev.pages.length) {
        return prev
      }

      const pages = [...prev.pages]
      const [page] = pages.splice(index, 1)
      pages.splice(target, 0, page)
      return { ...prev, pages }
    })
  }

  function removeActivePage() {
    if (!activePage || config.pages.length <= 1) {
      return
    }

    const pageIndex = config.pages.findIndex((page) => page.id === activePage.id)
    const nextActivePage = config.pages[pageIndex + 1] ?? config.pages[pageIndex - 1] ?? null

    setConfig((prev) => ({
      ...prev,
      pages: prev.pages.filter((page) => page.id !== activePage.id),
      blocks: prev.blocks.filter((block) => block.pageId !== activePage.id),
    }))

    if (nextActivePage) {
      setActivePageId(nextActivePage.id)
    }

    setSelectedIds([])
    setIsInspectorOpen(false)
  }

  function moveSelectedToPage(pageId: string) {
    if (!activePage || !pageId || pageId === activePage.id || selectedIds.length === 0) {
      return
    }

    const movedIds = [...selectedIds]

    updateSelectedBlocks((block) => ({
      ...block,
      pageId,
    }))
    setActivePageId(pageId)
    setSelectedIds(movedIds)
  }

  const removeSelectedBlock = useCallback(() => {
    if (selectedIds.length === 0) {
      return
    }

    setConfig((prev) => ({
      ...prev,
      blocks: prev.blocks.filter((block) => !selectedIds.includes(block.id)),
    }))
    setSelectedIds([])
  }, [selectedIds])

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
    if (!selectedId || !activePage) {
      return
    }

    setConfig((prev) => {
      const pageIndexes = prev.blocks
        .map((block, index) => (block.pageId === activePage.id ? index : -1))
        .filter((index) => index >= 0)
      const pagePosition = pageIndexes.findIndex((index) => prev.blocks[index]?.id === selectedId)

      if (pagePosition < 0) {
        return prev
      }

      const targetPosition = pagePosition + offset
      if (targetPosition < 0 || targetPosition >= pageIndexes.length) {
        return prev
      }

      const sourceIndex = pageIndexes[pagePosition]
      const targetIndex = pageIndexes[targetPosition]
      const copy = [...prev.blocks]
      const item = copy[sourceIndex]
      copy[sourceIndex] = copy[targetIndex]
      copy[targetIndex] = item
      return { ...prev, blocks: copy }
    })
  }

  const duplicateSelected = useCallback(() => {
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
            id: nextBlockId(),
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
  }, [selectedIds])

  const groupSelected = useCallback(() => {
    if (selectedIds.length < 2) {
      return
    }

    const groupId = nextGroupId()

    setConfig((prev) => ({
      ...prev,
      blocks: prev.blocks.map((block) =>
        selectedIds.includes(block.id) ? { ...block, groupId } : block,
      ),
    }))
  }, [selectedIds])

  const ungroupSelected = useCallback(() => {
    if (selectedIds.length === 0) {
      return
    }

    setConfig((prev) => ({
      ...prev,
      blocks: prev.blocks.map((block) =>
        selectedIds.includes(block.id) ? { ...block, groupId: undefined } : block,
      ),
    }))
  }, [selectedIds])

  useEffect(() => {
    selectedIdsRef.current = selectedIds
    selectedGroupIdsRef.current = selectedGroupIds
    removeSelectedBlockRef.current = removeSelectedBlock
    duplicateSelectedRef.current = duplicateSelected
    groupSelectedRef.current = groupSelected
    ungroupSelectedRef.current = ungroupSelected
  }, [duplicateSelected, groupSelected, removeSelectedBlock, selectedGroupIds, selectedIds, ungroupSelected])

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (isTextEntryTarget(event.target)) {
        return
      }

      const isMeta = event.metaKey || event.ctrlKey
      const key = event.key.toLowerCase()
      const currentSelectedIds = selectedIdsRef.current
      const currentSelectedGroupIds = selectedGroupIdsRef.current

      if ((event.key === 'Delete' || event.key === 'Backspace') && currentSelectedIds.length > 0) {
        event.preventDefault()
        removeSelectedBlockRef.current()
        return
      }

      if (isMeta && !event.shiftKey && key === 'd' && currentSelectedIds.length > 0) {
        event.preventDefault()
        duplicateSelectedRef.current()
        return
      }

      if (isMeta && !event.shiftKey && key === 'g' && currentSelectedIds.length > 1) {
        event.preventDefault()
        groupSelectedRef.current()
        return
      }

      if (isMeta && event.shiftKey && key === 'g' && currentSelectedGroupIds.length > 0) {
        event.preventDefault()
        ungroupSelectedRef.current()
      }
    }

    window.addEventListener('keydown', handleKeyDown)

    return () => {
      window.removeEventListener('keydown', handleKeyDown)
    }
  }, [])

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

  function saveDraftNow() {
    const savedAt = saveDraftConfig(config)
    if (savedAt) {
      setLastSavedAt(savedAt)
    }
  }

  function clearDraftNow() {
    clearDraftConfig()
    setLastSavedAt(null)
  }

  function downloadProjectFile() {
    const content = createProjectFileContent(config)
    const blob = new Blob([content], { type: 'application/json;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = `${config.name || 'report'}.frpt.json`
    anchor.click()
    URL.revokeObjectURL(url)
  }

  function requestProjectImport() {
    projectFileInputRef.current?.click()
  }

  function loadProjectFile(file: File | null) {
    if (!file) {
      return
    }

    const reader = new FileReader()

    reader.onload = () => {
      const content = typeof reader.result === 'string' ? reader.result : ''
      const parsedConfig = parseProjectFileContent(content)

      if (!parsedConfig) {
        window.alert(copy.importProjectError)
        return
      }

      setConfig(parsedConfig)
      setActivePageId(parsedConfig.pages[0]?.id ?? 'p1')
      setSelectedIds([])
      const savedAt = saveDraftConfig(parsedConfig)
      if (savedAt) {
        setLastSavedAt(savedAt)
      }
    }

    reader.readAsText(file)
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
          <div className="canvas-block-chip">{copy.text}</div>
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
          <div className="canvas-block-chip">{copy.line}</div>
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
          <div className="canvas-block-chip">{copy.spacer}</div>
          <div className="canvas-spacer">{copy.spacerSummary(block.size)}</div>
        </>
      )
    }

    if (block.type === 'image') {
      return (
        <>
          <div className="canvas-block-chip">{copy.image}</div>
          {block.source ? (
            <img
              className="canvas-image"
              src={block.source}
              alt={block.alt ?? copy.selectedImageAlt}
              style={{ objectFit: block.fit === 'none' ? 'fill' : block.fit }}
            />
          ) : (
            <div className="canvas-image-placeholder">{copy.imagePlaceholder}</div>
          )}
        </>
      )
    }

    if (block.type === 'table') {
      const columnTemplate = block.columns.map((column) => `minmax(${Math.max(column.width, 1)}fr, 1fr)`).join(' ')
      const previewRows = 2

      return (
        <>
          <div className="canvas-block-chip">{copy.table}</div>
          <div className="canvas-table-preview" style={{ gridTemplateColumns: columnTemplate }}>
            {block.columns.map((column) => (
              <div key={`${block.id}-${column.id}-header`} className="canvas-table-cell header">
                {column.header}
              </div>
            ))}
            {Array.from({ length: previewRows }).flatMap((_, rowIndex) =>
              block.columns.map((column) => (
                <div key={`${block.id}-${column.id}-${rowIndex}`} className="canvas-table-cell" style={{ textAlign: column.align ?? 'left' }}>
                  {`{{ row.${column.field} }}`}
                </div>
              )),
            )}
          </div>
        </>
      )
    }

    if (block.type === 'repeat') {
      const previewItems = Array.from({ length: 3 }, (_, index) => index)

      return (
        <>
          <div className="canvas-block-chip">{copy.repeat}</div>
          <div className="canvas-repeat-preview" style={{ gap: `${block.itemGap}px` }}>
            {previewItems.map((itemIndex) => (
              <div key={`${block.id}-item-${itemIndex}`} className="canvas-repeat-item">
                {block.itemTemplate.split('\n').map((line, lineIndex) => (
                  <span key={`${block.id}-item-${itemIndex}-line-${lineIndex}`}>{line}</span>
                ))}
              </div>
            ))}
          </div>
        </>
      )
    }

    return (
      <>
        <div className="canvas-block-chip">{copy.pageBreak}</div>
        <div className="canvas-page-break">{copy.pageBreak}</div>
      </>
    )
  }

  function renderInspectorContent() {
    if (!selectedBlock) {
      return (
        <div className="empty-state">
          <strong>{copy.noSelectionInspector}</strong>
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
                <span className="field-label">{copy.width}</span>
              <input
                type="number"
                value={Math.round(selectedBlock.frame.width)}
                onChange={(e) => updateSelectedFrame({ width: Number(e.target.value) || 0 })}
              />
            </label>
            <label>
                <span className="field-label">{copy.height}</span>
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
                <h3>{copy.content}</h3>
              </div>
            </div>
            <div className="field-stack">
              <label>
                <span className="field-label">{copy.text}</span>
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
                <span className="field-label">{copy.styleRef}</span>
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
                <span className="field-label">{copy.textAlign}</span>
                <select
                  value={selectedBlock.align ?? 'left'}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'text' ? { ...block, align: e.target.value as Align } : block,
                    )
                  }
                >
                  <option value="left">{copy.leftShort}</option>
                  <option value="center">{copy.centerShort}</option>
                  <option value="right">{copy.rightShort}</option>
                  <option value="justify">{copy.justifyShort}</option>
                </select>
              </label>
            </div>
          </section>

          <section className="panel-section">
            <div className="section-heading">
              <div><h3>{copy.typography}</h3></div>
            </div>
            <div className="field-stack">
              <div className="field-row">
                <label className="field-grow">
                  <span className="field-label">{copy.size}</span>
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
                  <span className="field-label">{copy.color}</span>
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
                  <span>{copy.bold}</span>
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
                  <span>{copy.italic}</span>
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
                <h3>{copy.appearance}</h3>
              </div>
            </div>
            <div className="field-stack">
              <label>
                <span className="field-label">{copy.thickness}</span>
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
                <span className="field-label">{copy.color}</span>
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
                <h3>{copy.spacing}</h3>
              </div>
            </div>
            <div className="field-stack">
              <label>
                <span className="field-label">{copy.size}</span>
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
              <strong>{copy.unconfiguredBlock}</strong>
            </div>
          </section>
        )}

        {selectedBlock.type === 'image' && (
          <section className="panel-section">
            <div className="section-heading">
              <div>
                <h3>{copy.image}</h3>
              </div>
            </div>
            <div className="field-stack">
              <label>
                <span className="field-label">{copy.source}</span>
                <input
                  value={selectedBlock.source}
                  placeholder={copy.sourcePlaceholder}
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
                  <span className="field-label">{copy.fit}</span>
                  <select
                    value={selectedBlock.fit}
                    onChange={(e) =>
                      updateSelectedBlock((block) =>
                        block.type === 'image' ? { ...block, fit: e.target.value as ImageFit } : block,
                      )
                    }
                  >
                    <option value="contain">{copy.contain}</option>
                    <option value="cover">{copy.cover}</option>
                    <option value="none">{copy.none}</option>
                  </select>
                </label>
                <label className="field-grow">
                  <span className="field-label">{copy.alt}</span>
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
                {copy.loadImage}
              </button>
            </div>
          </section>
        )}

        {selectedBlock.type === 'table' && (
          <section className="panel-section">
            <div className="section-heading">
              <div>
                <h3>{copy.table}</h3>
              </div>
            </div>
            <div className="field-stack">
              <label>
                <span className="field-label">{copy.tableName}</span>
                <input
                  value={selectedBlock.name}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'table' ? { ...block, name: e.target.value || 'table-block' } : block,
                    )
                  }
                />
              </label>
              <label>
                <span className="field-label">{copy.dataSource}</span>
                <input
                  value={selectedBlock.dataSource}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'table' ? { ...block, dataSource: e.target.value || 'items' } : block,
                    )
                  }
                />
              </label>
              <label>
                <span className="field-label">{copy.columns}</span>
                <textarea
                  rows={6}
                  value={formatTableColumns(selectedBlock.columns)}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'table'
                        ? { ...block, columns: parseTableColumns(e.target.value, block.columns) }
                        : block,
                    )
                  }
                />
              </label>
              <div className="field-row">
                <label className="field-grow">
                  <span className="field-label">{copy.growth}</span>
                  <select
                    value={selectedBlock.growthMode}
                    onChange={(e) =>
                      updateSelectedBlock((block) =>
                        block.type === 'table'
                          ? { ...block, growthMode: e.target.value as 'fixed' | 'grow' }
                          : block,
                      )
                    }
                  >
                    <option value="grow">{copy.growDown}</option>
                    <option value="fixed">{copy.fixedFrame}</option>
                  </select>
                </label>
                <label className="field-grow">
                  <span className="field-label">{copy.overflow}</span>
                  <select
                    value={selectedBlock.overflowMode}
                    onChange={(e) =>
                      updateSelectedBlock((block) =>
                        block.type === 'table'
                          ? { ...block, overflowMode: e.target.value as 'nextPage' | 'truncate' }
                          : block,
                      )
                    }
                  >
                    <option value="nextPage">{copy.nextPage}</option>
                    <option value="truncate">{copy.truncate}</option>
                  </select>
                </label>
              </div>
              <label className="field-toggle">
                <input
                  type="checkbox"
                  checked={selectedBlock.keepTogether}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'table' ? { ...block, keepTogether: e.target.checked } : block,
                    )
                  }
                />
                <span>{copy.keepTogetherBeforeBreak}</span>
              </label>
              <small className="field-help">{copy.tableColumnsHelp}</small>
              <small className="field-help">{copy.growthHelp}</small>
            </div>
          </section>
        )}

        {selectedBlock.type === 'repeat' && (
          <section className="panel-section">
            <div className="section-heading">
              <div>
                <h3>{copy.repeat}</h3>
              </div>
            </div>
            <div className="field-stack">
              <label>
                <span className="field-label">{copy.repeatName}</span>
                <input
                  value={selectedBlock.name}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'repeat' ? { ...block, name: e.target.value || 'repeat-block' } : block,
                    )
                  }
                />
              </label>
              <label>
                <span className="field-label">{copy.dataSource}</span>
                <input
                  value={selectedBlock.dataSource}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'repeat' ? { ...block, dataSource: e.target.value || 'items' } : block,
                    )
                  }
                />
              </label>
              <label>
                <span className="field-label">{copy.template}</span>
                <textarea
                  rows={5}
                  value={selectedBlock.itemTemplate}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'repeat' ? { ...block, itemTemplate: e.target.value } : block,
                    )
                  }
                />
              </label>
              <label>
                <span className="field-label">{copy.gap}</span>
                <input
                  type="number"
                  min={0}
                  max={64}
                  value={selectedBlock.itemGap}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'repeat' ? { ...block, itemGap: Number(e.target.value) || 0 } : block,
                    )
                  }
                />
              </label>
              <div className="field-row">
                <label className="field-grow">
                  <span className="field-label">{copy.growth}</span>
                  <select
                    value={selectedBlock.growthMode}
                    onChange={(e) =>
                      updateSelectedBlock((block) =>
                        block.type === 'repeat'
                          ? { ...block, growthMode: e.target.value as 'fixed' | 'grow' }
                          : block,
                      )
                    }
                  >
                    <option value="grow">{copy.growDown}</option>
                    <option value="fixed">{copy.fixedFrame}</option>
                  </select>
                </label>
                <label className="field-grow">
                  <span className="field-label">{copy.overflow}</span>
                  <select
                    value={selectedBlock.overflowMode}
                    onChange={(e) =>
                      updateSelectedBlock((block) =>
                        block.type === 'repeat'
                          ? { ...block, overflowMode: e.target.value as 'nextPage' | 'truncate' }
                          : block,
                      )
                    }
                  >
                    <option value="nextPage">{copy.nextPage}</option>
                    <option value="truncate">{copy.truncate}</option>
                  </select>
                </label>
              </div>
              <label className="field-toggle">
                <input
                  type="checkbox"
                  checked={selectedBlock.keepTogether}
                  onChange={(e) =>
                    updateSelectedBlock((block) =>
                      block.type === 'repeat' ? { ...block, keepTogether: e.target.checked } : block,
                    )
                  }
                />
                <span>{copy.keepTogetherBeforeBreak}</span>
              </label>
            </div>
          </section>
        )}

        <section className="panel-section">
          <div className="section-heading">
            <div>
              <h3>{copy.actions}</h3>
            </div>
          </div>
          <div className="property-actions">
            <button type="button" onClick={duplicateSelected}>
              {copy.duplicate}
            </button>
            <button type="button" onClick={() => moveSelected(-1)}>
              {copy.moveLayerUp}
            </button>
            <button type="button" onClick={() => moveSelected(1)}>
              {copy.moveLayerDown}
            </button>
            <button type="button" className="danger" onClick={removeSelectedBlock}>
              {copy.deleteSelection}
            </button>
          </div>
        </section>
      </>
    )
  }

  function renderContextualToolbar() {
    if (selectedIds.length === 0) {
      return (
        <section className="toolbar toolbar-context" aria-label={copy.contextToolbarAria}>
          <div className="toolbar-context-empty">
            {copy.emptySelectionHint}
          </div>
        </section>
      )
    }

    const activeTextBlock = selectionType === 'text' && selectedBlock?.type === 'text' ? selectedBlock : null
    const activeLineBlock = selectionType === 'line' && selectedBlock?.type === 'line' ? selectedBlock : null
    const activeSpacerBlock = selectionType === 'spacer' && selectedBlock?.type === 'spacer' ? selectedBlock : null
    const activeImageBlock = selectionType === 'image' && selectedBlock?.type === 'image' ? selectedBlock : null
    const activeTableBlock = selectionType === 'table' && selectedBlock?.type === 'table' ? selectedBlock : null
    const activeRepeatBlock = selectionType === 'repeat' && selectedBlock?.type === 'repeat' ? selectedBlock : null

    return (
      <section className="toolbar toolbar-context" aria-label={copy.contextToolbarAria}>
        <div className="toolbar-group toolbar-context-summary">
          <span className="status-pill accent">
            {copy.selectedStatus(selectedIds.length)}
          </span>
          <span className="toolbar-info">
            {selectionType ? getBlockTypeLabel(selectionType, locale) : copy.mixedSelection}
          </span>
          {selectedGroupIds.length === 1 && <span className="toolbar-info">{copy.groupLabel(selectedGroupIds[0])}</span>}
          {selectionBounds && (
            <span className="toolbar-info">
              {Math.round(selectionBounds.width)} x {Math.round(selectionBounds.height)}
            </span>
          )}
        </div>

        <div className="toolbar-sep" />

        <div className="toolbar-group">
          <span className="toolbar-label">{copy.selection}</span>
          <TooltipButton type="button" className="toolbar-button" onClick={duplicateSelected} tooltip={copy.duplicate}>
            {copy.duplicate}
          </TooltipButton>
          <TooltipButton
            type="button"
            className="toolbar-button"
            onClick={groupSelected}
            disabled={selectedIds.length < 2}
            tooltip={copy.group}
          >
            {copy.group}
          </TooltipButton>
          <TooltipButton
            type="button"
            className="toolbar-button"
            onClick={ungroupSelected}
            disabled={selectedGroupIds.length === 0}
            tooltip={copy.ungroup}
          >
            {copy.ungroup}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button danger" onClick={removeSelectedBlock} tooltip={copy.deleteSelection}>
            {copy.deleteSelection}
          </TooltipButton>
        </div>

        <div className="toolbar-sep" />

        <div className="toolbar-group">
          <span className="toolbar-label">{copy.pageTools}</span>
          <TooltipButton type="button" className="toolbar-button" onClick={() => alignSelectionToPage('left')} tooltip={copy.leftMargin}>
            {copy.leftMargin}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={() => alignSelectionToPage('center')} tooltip={copy.center}>
            {copy.center}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={() => alignSelectionToPage('right')} tooltip={copy.rightMargin}>
            {copy.rightMargin}
          </TooltipButton>
        </div>

        {isGroupSelection && (
          <>
            <div className="toolbar-sep" />

            <div className="toolbar-group">
              <span className="toolbar-label">{copy.layout}</span>
              <TooltipButton type="button" className="toolbar-button" onClick={() => alignSelection('left')} tooltip={copy.alignLeft}>
                {copy.alignLeft}
              </TooltipButton>
              <TooltipButton type="button" className="toolbar-button" onClick={() => alignSelection('center')} tooltip={copy.centerX}>
                {copy.centerX}
              </TooltipButton>
              <TooltipButton type="button" className="toolbar-button" onClick={() => alignSelection('right')} tooltip={copy.alignRight}>
                {copy.alignRight}
              </TooltipButton>
              <TooltipButton type="button" className="toolbar-button" onClick={() => alignSelection('top')} tooltip={copy.top}>
                {copy.top}
              </TooltipButton>
              <TooltipButton type="button" className="toolbar-button" onClick={() => alignSelection('middle')} tooltip={copy.centerY}>
                {copy.centerY}
              </TooltipButton>
              <TooltipButton type="button" className="toolbar-button" onClick={() => alignSelection('bottom')} tooltip={copy.bottom}>
                {copy.bottom}
              </TooltipButton>
              <TooltipButton type="button" className="toolbar-button" onClick={() => distributeSelection('horizontal')} disabled={selectedBlocks.length < 3} tooltip={copy.distributeX}>
                {copy.distributeX}
              </TooltipButton>
              <TooltipButton type="button" className="toolbar-button" onClick={() => distributeSelection('vertical')} disabled={selectedBlocks.length < 3} tooltip={copy.distributeY}>
                {copy.distributeY}
              </TooltipButton>
              <TooltipButton type="button" className="toolbar-button" onClick={() => matchSelectionSize('width')} disabled={selectedBlocks.length < 2} tooltip={copy.matchWidth}>
                {copy.matchWidth}
              </TooltipButton>
              <TooltipButton type="button" className="toolbar-button" onClick={() => matchSelectionSize('height')} disabled={selectedBlocks.length < 2} tooltip={copy.matchHeight}>
                {copy.matchHeight}
              </TooltipButton>
            </div>
          </>
        )}

        {selectionType === 'text' && activeTextBlock && (
          <>
            <div className="toolbar-sep" />

            <div className="toolbar-group toolbar-inline-fields">
              <span className="toolbar-label">{copy.text}</span>
              <div className="toolbar-segment" role="group" aria-label={copy.textAlign}>
                <TooltipButton type="button" className="toolbar-button" onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, align: 'left' } : block)} tooltip={`${copy.textAlign}: ${copy.leftShort}`}>
                  {copy.leftShort}
                </TooltipButton>
                <TooltipButton type="button" className="toolbar-button" onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, align: 'center' } : block)} tooltip={`${copy.textAlign}: ${copy.centerShort}`}>
                  {copy.centerShort}
                </TooltipButton>
                <TooltipButton type="button" className="toolbar-button" onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, align: 'right' } : block)} tooltip={`${copy.textAlign}: ${copy.rightShort}`}>
                  {copy.rightShort}
                </TooltipButton>
                <TooltipButton type="button" className="toolbar-button" onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, align: 'justify' } : block)} tooltip={`${copy.textAlign}: ${copy.justifyShort}`}>
                  {copy.justifyShort}
                </TooltipButton>
              </div>
              <label className="toolbar-field toolbar-field-narrow">
                <span>{copy.size}</span>
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
                <span>{copy.styleRef}</span>
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
                <span>{copy.color}</span>
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
              <TooltipButton type="button" className={activeTextBlock.bold ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'} onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, bold: !block.bold || undefined } : block)} tooltip={copy.bold}>
                {copy.bold}
              </TooltipButton>
              <TooltipButton type="button" className={activeTextBlock.italic ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'} onClick={() => updateSelectedBlocks((block) => block.type === 'text' ? { ...block, italic: !block.italic || undefined } : block)} tooltip={copy.italic}>
                {copy.italic}
              </TooltipButton>
            </div>
          </>
        )}

        {selectionType === 'line' && activeLineBlock && (
          <>
            <div className="toolbar-sep" />

            <div className="toolbar-group toolbar-inline-fields">
              <span className="toolbar-label">{copy.line}</span>
              <label className="toolbar-field toolbar-field-narrow">
                <span>{copy.thickness}</span>
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
                <span>{copy.color}</span>
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
              <span className="toolbar-label">{copy.spacer}</span>
              {[4, 8, 12, 16, 24, 32].map((value) => (
                <TooltipButton
                  type="button"
                  key={value}
                  className="toolbar-button"
                  onClick={() => updateSelectedBlocks((block) => block.type === 'spacer' ? { ...block, size: value } : block)}
                  tooltip={`${copy.spacer}: ${value}px`}
                >
                  {value}px
                </TooltipButton>
              ))}
            </div>
          </>
        )}

        {selectionType === 'image' && activeImageBlock && (
          <>
            <div className="toolbar-sep" />

            <div className="toolbar-group toolbar-inline-fields">
              <span className="toolbar-label">{copy.image}</span>
              <TooltipButton type="button" className="toolbar-button" onClick={requestImageUpload} tooltip={copy.loadImage}>
                {copy.upload}
              </TooltipButton>
              <div className="toolbar-segment" role="group" aria-label={copy.imageFit}>
                <TooltipButton type="button" className={activeImageBlock.fit === 'contain' ? 'toolbar-button toolbar-toggle active' : 'toolbar-button'} onClick={() => updateSelectedBlocks((block) => block.type === 'image' ? { ...block, fit: 'contain' } : block)} tooltip={`${copy.imageFit}: ${copy.contain}`}>
                  {copy.contain}
                </TooltipButton>
                <TooltipButton type="button" className={activeImageBlock.fit === 'cover' ? 'toolbar-button toolbar-toggle active' : 'toolbar-button'} onClick={() => updateSelectedBlocks((block) => block.type === 'image' ? { ...block, fit: 'cover' } : block)} tooltip={`${copy.imageFit}: ${copy.cover}`}>
                  {copy.cover}
                </TooltipButton>
                <TooltipButton type="button" className={activeImageBlock.fit === 'none' ? 'toolbar-button toolbar-toggle active' : 'toolbar-button'} onClick={() => updateSelectedBlocks((block) => block.type === 'image' ? { ...block, fit: 'none' } : block)} tooltip={`${copy.imageFit}: ${copy.none}`}>
                  {copy.none}
                </TooltipButton>
              </div>
              <label className="toolbar-field toolbar-field-wide">
                <span>{copy.source}</span>
                <input
                  value={activeImageBlock.source}
                  placeholder={copy.sourcePlaceholder}
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

        {selectionType === 'table' && activeTableBlock && (
          <>
            <div className="toolbar-sep" />

            <div className="toolbar-group toolbar-inline-fields">
              <span className="toolbar-label">{copy.table}</span>
              <label className="toolbar-field toolbar-field-medium">
                <span>{copy.source}</span>
                <input
                  value={activeTableBlock.dataSource}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'table' ? { ...block, dataSource: e.target.value || 'items' } : block,
                    )
                  }
                />
              </label>
              <label className="toolbar-field toolbar-field-medium">
                <span>{copy.growth}</span>
                <select
                  value={activeTableBlock.growthMode}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'table'
                        ? { ...block, growthMode: e.target.value as 'fixed' | 'grow' }
                        : block,
                    )
                  }
                >
                  <option value="grow">{copy.growDown}</option>
                  <option value="fixed">{copy.fixed}</option>
                </select>
              </label>
              <label className="toolbar-field toolbar-field-medium">
                <span>{copy.overflow}</span>
                <select
                  value={activeTableBlock.overflowMode}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'table'
                        ? { ...block, overflowMode: e.target.value as 'nextPage' | 'truncate' }
                        : block,
                    )
                  }
                >
                  <option value="nextPage">{copy.nextPage}</option>
                  <option value="truncate">{copy.truncate}</option>
                </select>
              </label>
              <span className="toolbar-info">{copy.columnsCount(activeTableBlock.columns.length)}</span>
              <TooltipButton
                type="button"
                className={activeTableBlock.keepTogether ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'}
                onClick={() =>
                  updateSelectedBlocks((block) =>
                    block.type === 'table' ? { ...block, keepTogether: !block.keepTogether } : block,
                  )
                }
                tooltip={copy.keepTogether}
              >
                {copy.keepTogether}
              </TooltipButton>
            </div>
          </>
        )}

        {selectionType === 'repeat' && activeRepeatBlock && (
          <>
            <div className="toolbar-sep" />

            <div className="toolbar-group toolbar-inline-fields">
              <span className="toolbar-label">{copy.repeat}</span>
              <label className="toolbar-field toolbar-field-medium">
                <span>{copy.source}</span>
                <input
                  value={activeRepeatBlock.dataSource}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'repeat' ? { ...block, dataSource: e.target.value || 'items' } : block,
                    )
                  }
                />
              </label>
              <label className="toolbar-field toolbar-field-narrow">
                <span>{copy.gap}</span>
                <input
                  type="number"
                  min={0}
                  max={64}
                  value={activeRepeatBlock.itemGap}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'repeat' ? { ...block, itemGap: Number(e.target.value) || 0 } : block,
                    )
                  }
                />
              </label>
              <label className="toolbar-field toolbar-field-medium">
                <span>{copy.growth}</span>
                <select
                  value={activeRepeatBlock.growthMode}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'repeat'
                        ? { ...block, growthMode: e.target.value as 'fixed' | 'grow' }
                        : block,
                    )
                  }
                >
                  <option value="grow">{copy.growDown}</option>
                  <option value="fixed">{copy.fixed}</option>
                </select>
              </label>
              <label className="toolbar-field toolbar-field-medium">
                <span>{copy.overflow}</span>
                <select
                  value={activeRepeatBlock.overflowMode}
                  onChange={(e) =>
                    updateSelectedBlocks((block) =>
                      block.type === 'repeat'
                        ? { ...block, overflowMode: e.target.value as 'nextPage' | 'truncate' }
                        : block,
                    )
                  }
                >
                  <option value="nextPage">{copy.nextPage}</option>
                  <option value="truncate">{copy.truncate}</option>
                </select>
              </label>
              <TooltipButton
                type="button"
                className={activeRepeatBlock.keepTogether ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'}
                onClick={() =>
                  updateSelectedBlocks((block) =>
                    block.type === 'repeat' ? { ...block, keepTogether: !block.keepTogether } : block,
                  )
                }
                tooltip={copy.keepTogether}
              >
                {copy.keepTogether}
              </TooltipButton>
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
          <h1>{copy.appTitle}</h1>
        </div>
        <div className="topbar-meta">
          <span className="status-pill">{config.name || copy.untitledReport}</span>
          <span className="status-pill">{copy.pageStatus(activePageIndex + 1, config.pages.length)}</span>
          <span className="status-pill">{copy.blocksStatus(currentPageBlocks.length)}</span>
          <span className="status-pill accent">
            {selectedIds.length > 0 ? copy.selectedStatus(selectedIds.length) : copy.noSelectionStatus}
          </span>
          <span className="status-pill">{savedTimeLabel}</span>
        </div>
        <div className="topbar-controls">
          <label className="topbar-language">
            <span>{copy.language}</span>
            <select
              value={locale}
              onChange={(event) => {
                if (isLocale(event.target.value)) {
                  setLocale(event.target.value)
                }
              }}
            >
              {LOCALE_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </label>
        </div>
      </header>

      <section className="toolbar toolbar-main" aria-label={copy.editorToolbarAria}>
        {/* LEFT: document + layers dropdowns */}
        <div className="toolbar-group">
          <div className="toolbar-dropdown-wrap">
            <TooltipButton
              ref={docBtnRef}
              type="button"
              className={showDocPanel ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'}
              onClick={() => { setShowDocPanel(v => !v); setShowLayersPanel(false) }}
              tooltip={copy.document}
            >
              <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M3 2h7l3 3v9H3V2z" stroke="currentColor" strokeWidth="1.4" strokeLinejoin="round" fill="none"/>
                <path d="M10 2v3h3" stroke="currentColor" strokeWidth="1.4" strokeLinejoin="round"/>
                <path d="M5 7h6M5 9.5h4" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/>
              </svg>
              {copy.document}
              <svg width="10" height="10" viewBox="0 0 10 10" fill="none" xmlns="http://www.w3.org/2000/svg" style={{marginLeft:2}}>
                <path d="M2 3.5l3 3 3-3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            </TooltipButton>
            {showDocPanel && (
              <div className="toolbar-dropdown">
                <div className="toolbar-dropdown-section">
                  <div className="toolbar-dropdown-label">{copy.metadata}</div>
                  <label className="toolbar-dropdown-field">
                    <span>{copy.name}</span>
                    <input value={config.name} onChange={(e) => setConfig((prev) => ({ ...prev, name: e.target.value }))} />
                  </label>
                  <label className="toolbar-dropdown-field">
                    <span>{copy.title}</span>
                    <input value={config.title} onChange={(e) => setConfig((prev) => ({ ...prev, title: e.target.value }))} />
                  </label>
                  <label className="toolbar-dropdown-field">
                    <span>{copy.companyParam}</span>
                    <input value={config.companyParam} onChange={(e) => setConfig((prev) => ({ ...prev, companyParam: e.target.value }))} />
                  </label>
                  <label className="toolbar-dropdown-field">
                    <span>{copy.periodParam}</span>
                    <input value={config.periodParam} onChange={(e) => setConfig((prev) => ({ ...prev, periodParam: e.target.value }))} />
                  </label>
                  <label className="toolbar-dropdown-field">
                    <span>{copy.dataSources}</span>
                    <textarea
                      rows={4}
                      value={formatNameList(config.dataSources)}
                      onChange={(e) => setConfig((prev) => ({ ...prev, dataSources: parseNameList(e.target.value) }))}
                    />
                  </label>
                  <div className="toolbar-dropdown-label">{copy.html}</div>
                  <label className="toolbar-dropdown-field">
                    <span>{copy.maxWidth}</span>
                    <input
                      type="number"
                      min={1}
                      value={config.htmlRendererOptions.maxWidth}
                      onChange={(e) =>
                        setConfig((prev) => ({
                          ...prev,
                          htmlRendererOptions: {
                            ...prev.htmlRendererOptions,
                            maxWidth: Number(e.target.value) || DEFAULT_HTML_RENDERER_OPTIONS.maxWidth,
                          },
                        }))
                      }
                    />
                  </label>
                  <label className="toolbar-dropdown-field">
                    <span>{copy.fontFamily}</span>
                    <input
                      value={config.htmlRendererOptions.fontFamily}
                      onChange={(e) =>
                        setConfig((prev) => ({
                          ...prev,
                          htmlRendererOptions: {
                            ...prev.htmlRendererOptions,
                            fontFamily: e.target.value || DEFAULT_HTML_RENDERER_OPTIONS.fontFamily,
                          },
                        }))
                      }
                    />
                  </label>
                  <label className="toolbar-dropdown-field">
                    <span>{copy.pageDivider}</span>
                    <textarea
                      rows={3}
                      value={config.htmlRendererOptions.pageDividerStyle}
                      onChange={(e) =>
                        setConfig((prev) => ({
                          ...prev,
                          htmlRendererOptions: {
                            ...prev.htmlRendererOptions,
                            pageDividerStyle: e.target.value || DEFAULT_HTML_RENDERER_OPTIONS.pageDividerStyle,
                          },
                        }))
                      }
                    />
                  </label>
                  <label className="toolbar-dropdown-field" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <span>{copy.outlook}</span>
                    <input
                      type="checkbox"
                      checked={config.htmlRendererOptions.outlookCompatible}
                      onChange={(e) =>
                        setConfig((prev) => ({
                          ...prev,
                          htmlRendererOptions: {
                            ...prev.htmlRendererOptions,
                            outlookCompatible: e.target.checked,
                          },
                        }))
                      }
                    />
                  </label>
                  <label className="toolbar-dropdown-field" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <span>{copy.showPageNumbers}</span>
                    <input
                      type="checkbox"
                      checked={config.showPageNumbers !== false}
                      onChange={(e) =>
                        setConfig((prev) => ({
                          ...prev,
                          showPageNumbers: e.target.checked,
                        }))
                      }
                    />
                  </label>
                </div>
              </div>
            )}
          </div>

          <div className="toolbar-dropdown-wrap">
            <TooltipButton
              ref={layersBtnRef}
              type="button"
              className={showLayersPanel ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'}
              onClick={() => { setShowLayersPanel(v => !v); setShowDocPanel(false) }}
              tooltip={copy.layers}
            >
              <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M8 2L14 5.5 8 9 2 5.5 8 2z" stroke="currentColor" strokeWidth="1.4" strokeLinejoin="round" fill="none"/>
                <path d="M2 8.5l6 3.5 6-3.5" stroke="currentColor" strokeWidth="1.4" strokeLinejoin="round"/>
              </svg>
              {copy.layers}
              <span className="toolbar-badge">{currentPageBlocks.length}</span>
              <svg width="10" height="10" viewBox="0 0 10 10" fill="none" xmlns="http://www.w3.org/2000/svg" style={{marginLeft:2}}>
                <path d="M2 3.5l3 3 3-3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            </TooltipButton>
            {showLayersPanel && (
              <div className="toolbar-dropdown">
                <div className="toolbar-dropdown-section">
                  <div className="toolbar-dropdown-label">{copy.pageSummary(activePageIndex + 1, currentPageBlocks.length, selectedIds.length)}</div>
                  <div className="block-list">
                    {currentPageBlocks.map((block, index) => (
                      <button
                        type="button"
                        key={block.id}
                        className={selectedId === block.id ? 'block-row selected' : 'block-row'}
                        onClick={(event) => handleSelectionPointer(block.id, event)}
                      >
                        <span className="block-index">{index + 1}</span>
                        <span className="block-row-copy">
                          <strong>{getBlockTypeLabel(block.type, locale)}</strong>
                          <small>{getBlockSummary(block, locale)}{block.groupId ? ` · ${block.groupId}` : ''}</small>
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

        <div className="toolbar-group">
          <span className="toolbar-label">{copy.page}</span>
          <label className="toolbar-field toolbar-field-medium">
            <span>{copy.active}</span>
            <select value={activePage?.id ?? ''} onChange={(e) => selectPage(e.target.value)}>
              {config.pages.map((page, index) => (
                <option key={page.id} value={page.id}>{copy.pageLabel(index + 1)}</option>
              ))}
            </select>
          </label>
          <TooltipButton type="button" className="toolbar-button" onClick={addPage} tooltip={copy.newPage}>
            {copy.newPage}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={() => moveActivePage(-1)} disabled={activePageIndex <= 0} tooltip={copy.moveUp}>
            {copy.moveUp}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={() => moveActivePage(1)} disabled={activePageIndex >= config.pages.length - 1} tooltip={copy.moveDown}>
            {copy.moveDown}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={removeActivePage} disabled={config.pages.length <= 1} tooltip={copy.deletePage}>
            {copy.deletePage}
          </TooltipButton>
          <label className="toolbar-field toolbar-field-medium">
            <span>{copy.moveSelection}</span>
            <select value={resolvedMoveTargetPageId} onChange={(e) => setMoveTargetPageId(e.target.value)} disabled={pageMoveOptions.length === 0}>
              {pageMoveOptions.length === 0 ? (
                <option value="">{copy.noDestination}</option>
              ) : (
                pageMoveOptions.map((page) => {
                  const absoluteIndex = config.pages.findIndex((candidate) => candidate.id === page.id)
                  return <option key={page.id} value={page.id}>{copy.pageLabel(absoluteIndex + 1)}</option>
                })
              )}
            </select>
          </label>
          <TooltipButton type="button" className="toolbar-button" onClick={() => moveSelectedToPage(resolvedMoveTargetPageId)} disabled={!resolvedMoveTargetPageId || selectedIds.length === 0} tooltip={copy.send}>
            {copy.send}
          </TooltipButton>
        </div>

        <div className="toolbar-sep" />

        {/* CENTER: insert blocks */}
        <div className="toolbar-group">
          <span className="toolbar-label">{copy.insert}</span>
          <TooltipButton type="button" className="toolbar-button" onClick={() => addBlock('text')} tooltip={copy.addText}>
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M2 4h12M8 4v9" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
            </svg>
            {copy.blockTypes.text}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={() => addBlock('line')} tooltip={copy.addLine}>
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M2 8h12" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
            </svg>
            {copy.blockTypes.line}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={() => addBlock('spacer')} tooltip={copy.addSpacer}>
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M3 5h10M3 11h10M8 5v6" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round"/>
            </svg>
            {copy.blockTypes.spacer}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={() => addBlock('pageBreak')} tooltip={copy.addPageBreak}>
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M2 8h12" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeDasharray="2 2"/>
              <path d="M5 5l-2 3 2 3M11 5l2 3-2 3" stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
            {copy.pageBreak}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={() => addBlock('image')} tooltip={copy.addImage}>
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="2" y="3" width="12" height="10" rx="1.5" stroke="currentColor" strokeWidth="1.3" />
              <circle cx="6" cy="6.5" r="1" fill="currentColor" />
              <path d="M4 11l2.5-2.5 2 2L10.5 8l1.5 3" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
            {copy.blockTypes.image}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={() => addBlock('table')} tooltip={copy.addTable}>
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="2" y="3" width="12" height="10" rx="1.4" stroke="currentColor" strokeWidth="1.2" />
              <path d="M2 6.5h12M2 10h12M6 3v10M10 3v10" stroke="currentColor" strokeWidth="1.1" strokeLinecap="round" />
            </svg>
            {copy.blockTypes.table}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={() => addBlock('repeat')} tooltip={copy.addRepeat}>
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="3" y="2.5" width="10" height="2.5" rx="1" stroke="currentColor" strokeWidth="1.1" />
              <rect x="3" y="6.75" width="10" height="2.5" rx="1" stroke="currentColor" strokeWidth="1.1" />
              <rect x="3" y="11" width="10" height="2.5" rx="1" stroke="currentColor" strokeWidth="1.1" />
            </svg>
            {copy.blockTypes.repeat}
          </TooltipButton>
        </div>

        <div className="toolbar-sep" />

        {/* ZOOM */}
        <div className="toolbar-group">
          <div className="toolbar-segment" role="group" aria-label={copy.zoom}>
            <TooltipButton type="button" className="toolbar-button toolbar-icon-button" onClick={() => stepZoom(-1)} tooltip={copy.zoomOut}>
              <svg width="12" height="12" viewBox="0 0 12 12" fill="none"><path d="M2 6h8" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round"/></svg>
            </TooltipButton>
            <TooltipButton type="button" className="toolbar-button toolbar-value-button" onClick={resetZoom} tooltip={copy.resetZoom}>
              {Math.round(canvasZoom * 100)}%
            </TooltipButton>
            <TooltipButton type="button" className="toolbar-button toolbar-icon-button" onClick={() => stepZoom(1)} tooltip={copy.zoomIn}>
              <svg width="12" height="12" viewBox="0 0 12 12" fill="none"><path d="M6 2v8M2 6h8" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round"/></svg>
            </TooltipButton>
          </div>
          <TooltipButton
            type="button"
            className={showRulers ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'}
            onClick={() => setShowRulers((v) => !v)}
            tooltip={copy.rulers}
          >
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="1" y="5" width="14" height="6" rx="1" stroke="currentColor" strokeWidth="1.3" fill="none"/>
              <path d="M4 5v2M7 5v3M10 5v2M13 5v2" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/>
            </svg>
            {copy.rulers}
          </TooltipButton>
        </div>

        <div className="toolbar-spacer" />

        {/* RIGHT: actions */}
        <div className="toolbar-group">
          <TooltipButton type="button" className="toolbar-button" onClick={saveDraftNow} tooltip={copy.saveDraftTitle}>
            {copy.saveDraft}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={clearDraftNow} tooltip={copy.clearDraftTitle}>
            {copy.clearDraft}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={requestProjectImport} tooltip={copy.importProjectTitle}>
            {copy.importProject}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={downloadProjectFile} tooltip={copy.exportProjectTitle}>
            {copy.exportProject}
          </TooltipButton>
          <TooltipButton
            type="button"
            className={showYamlPanel ? 'toolbar-button toolbar-toggle active' : 'toolbar-button toolbar-toggle'}
            onClick={() => setShowYamlPanel((value) => !value)}
            tooltip={showYamlPanel ? copy.hideYaml : copy.showYaml}
          >
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M3 4h10M3 8h10M3 12h10" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round"/>
            </svg>
            YAML
          </TooltipButton>
          <TooltipButton
            type="button"
            className="toolbar-button"
            onClick={() => openInspector()}
            disabled={!selectedBlock}
            tooltip={copy.openInspector}
          >
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="8" cy="8" r="5.5" stroke="currentColor" strokeWidth="1.4"/>
              <path d="M8 7v4M8 5.5v.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
            </svg>
            {copy.inspector}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button" onClick={copyYaml} tooltip={copy.copyYamlTitle}>
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="5" y="1" width="8" height="10" rx="1.5" stroke="currentColor" strokeWidth="1.3" fill="none"/>
              <rect x="2" y="4" width="8" height="10" rx="1.5" stroke="currentColor" strokeWidth="1.3" fill="none" style={{fill: 'var(--bg-a)'}}/>
            </svg>
            {copy.copyYaml}
          </TooltipButton>
          <TooltipButton type="button" className="toolbar-button primary" onClick={downloadYaml} tooltip={copy.downloadTitle}>
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M8 2v8M5 8l3 3 3-3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
              <path d="M2 13h12" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round"/>
            </svg>
            {copy.download}
          </TooltipButton>
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
                    ref={setCanvasViewportElement}
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
                          event.preventDefault()
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

                      {currentPageBlocks.map((block, index) => (
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
                          onMouseDown={(event) => {
                            handleSelectionPointer(block.id, event)
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

                      {selectionBounds && isGroupSelection && (
                        <div
                          className="canvas-selection-outline"
                          style={{
                            width: `${selectionBounds.width + selectionOutlineInset * 2}px`,
                            height: `${selectionBounds.height + selectionOutlineInset * 2}px`,
                            transform: `translate(${selectionBounds.x - selectionOutlineInset}px, ${selectionBounds.y - selectionOutlineInset}px)`,
                          }}
                        >
                          <span className="canvas-selection-outline-label">
                            {selectedWholeGroupId ? copy.groupLabel(selectedWholeGroupId) : copy.selectedStatus(selectedIds.length)}
                          </span>
                        </div>
                      )}

                      {selectedTargetSelectors.length === 1 && (
                        <Moveable
                          ref={moveableRef}
                          target={selectedTargetSelectors[0] ?? null}
                          container={pageElement}
                          origin={false}
                          draggable
                          resizable
                          snappable
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
                          onResize={(event: {
                            target: HTMLElement | SVGElement
                            width: number
                            height: number
                            drag: { beforeTranslate: number[] }
                          }) => {
                            handleResize(event)
                          }}
                        />
                      )}
                    </article>

                    <Selecto
                      container={canvasViewportElement ?? undefined}
                      dragContainer={canvasViewportElement ?? pageElement ?? undefined}
                      selectableTargets={['.canvas-block']}
                      selectByClick
                      selectFromInside={false}
                      continueSelect={false}
                      preventDefault
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

                        setSelectedIds(expandSelectionForBlocks(currentPageBlocks, nextSelection, nextSelection.at(-1) ?? null))
                      }}
                    />
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

      <input
        ref={projectFileInputRef}
        type="file"
        accept="application/json,.json,.frpt.json"
        className="visually-hidden"
        onChange={(event) => {
          loadProjectFile(event.target.files?.[0] ?? null)
          event.target.value = ''
        }}
      />

      {isInspectorOpen && (
        <div className="modal-backdrop" onClick={closeInspector}>
          <section className="panel modal-panel inspector-panel" onClick={(event) => event.stopPropagation()}>
            <div className="panel-header modal-header">
              <div>
                <h2>{copy.properties}</h2>
              </div>
              <div className="modal-actions">
                <span className={selectedBlock ? 'panel-badge accent' : 'panel-badge'}>
                  {selectedBlock ? getBlockTypeLabel(selectedBlock.type, locale) : copy.noSelectionStatus}
                </span>
                <button type="button" className="toolbar-button" onClick={closeInspector}>
                  {copy.close}
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
