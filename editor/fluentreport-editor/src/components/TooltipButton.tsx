import * as Tooltip from '@radix-ui/react-tooltip'
import { forwardRef, type ButtonHTMLAttributes } from 'react'

type TooltipButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  tooltip: string
}

const TooltipButton = forwardRef<HTMLButtonElement, TooltipButtonProps>(function TooltipButton(
  { tooltip, children, className, ...buttonProps },
  ref,
) {
  const ariaLabel = buttonProps['aria-label'] ?? tooltip

  return (
    <Tooltip.Provider delayDuration={180} skipDelayDuration={300}>
      <Tooltip.Root>
        <Tooltip.Trigger asChild>
          <button ref={ref} {...buttonProps} className={className} aria-label={ariaLabel}>
            {children}
          </button>
        </Tooltip.Trigger>
        <Tooltip.Portal>
          <Tooltip.Content className="toolbar-tooltip-content" side="top" sideOffset={10}>
            {tooltip}
            <Tooltip.Arrow className="toolbar-tooltip-arrow" width={10} height={5} />
          </Tooltip.Content>
        </Tooltip.Portal>
      </Tooltip.Root>
    </Tooltip.Provider>
  )
})

export { TooltipButton }
