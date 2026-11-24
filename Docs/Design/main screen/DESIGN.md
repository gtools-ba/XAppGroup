---
name: Obsidian Amber
colors:
  surface: '#121415'
  surface-dim: '#121415'
  surface-bright: '#38393a'
  surface-container-lowest: '#0c0e0f'
  surface-container-low: '#1a1c1d'
  surface-container: '#1e2021'
  surface-container-high: '#282a2b'
  surface-container-highest: '#333536'
  on-surface: '#e2e2e3'
  on-surface-variant: '#d8c3ad'
  inverse-surface: '#e2e2e3'
  inverse-on-surface: '#2f3132'
  outline: '#a08e7a'
  outline-variant: '#534434'
  surface-tint: '#ffb95f'
  primary: '#ffc174'
  on-primary: '#472a00'
  primary-container: '#f59e0b'
  on-primary-container: '#613b00'
  inverse-primary: '#855300'
  secondary: '#ffb599'
  on-secondary: '#5a1c00'
  secondary-container: '#f66018'
  on-secondary-container: '#4f1700'
  tertiary: '#8ed5ff'
  on-tertiary: '#00354a'
  tertiary-container: '#38bdf8'
  on-tertiary-container: '#004965'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#ffddb8'
  primary-fixed-dim: '#ffb95f'
  on-primary-fixed: '#2a1700'
  on-primary-fixed-variant: '#653e00'
  secondary-fixed: '#ffdbce'
  secondary-fixed-dim: '#ffb599'
  on-secondary-fixed: '#370e00'
  on-secondary-fixed-variant: '#7f2b00'
  tertiary-fixed: '#c4e7ff'
  tertiary-fixed-dim: '#7bd0ff'
  on-tertiary-fixed: '#001e2c'
  on-tertiary-fixed-variant: '#004c69'
  background: '#121415'
  on-background: '#e2e2e3'
  surface-variant: '#333536'
typography:
  headline-xl:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '600'
    lineHeight: 40px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
    letterSpacing: -0.015em
  headline-md:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 26px
    letterSpacing: -0.01em
  body-lg:
    fontFamily: Inter
    fontSize: 15px
    fontWeight: '400'
    lineHeight: 22px
  body-md:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 18px
  body-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-lg:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '500'
    lineHeight: 18px
  label-md:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '600'
    lineHeight: 14px
    letterSpacing: 0.06em
  label-sm:
    fontFamily: Inter
    fontSize: 10px
    fontWeight: '700'
    lineHeight: 12px
    letterSpacing: 0.04em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  gutter: 1rem
  gutter-lg: 1.5rem
  margin: 1.5rem
  margin-sm: 1rem
  margin-lg: 2rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 0.875rem
  space-lg: 1.25rem
  space-xl: 1.75rem
---

## Brand & Style

This design system embodies a focused, modern desktop utility aesthetic: sleek, high-precision, and calm. It pairs the focus of a dark utility surface with the warmth and approachability of amber-gold accenting.

### Emotional Response
- **Confidence & Control:** The interface feels engineered, dependable, and unobtrusive, empowering users to optimize and tune their system without anxiety.
- **Warm Clarity:** Softened charcoal surfaces reduce eye fatigue, while glowing amber cues guide actions without harsh alarmist tropes.
- **Refinement:** High typographic legibility paired with quiet, low-contrast structural strokes gives an impression of modern native software (Windows 11 Mica / Fluent-adjacent).

### Design Style
- **Dark Minimal Utility:** Deep obsidian backgrounds (`#0e1111`), subtly raised charcoal tiles (`#1a1d1e`), faint translucent keylines (`border-white/5` to `border-white/10`), and deliberate gold/amber status signals.

## Colors

The palette is engineered for prolonged desktop use with minimal visual noise and strategic focal points.

### Palette Roles
- **Canvas / Root Background (`#0e1111` / `#121415`):** Ultra-deep charcoal grounding the main frame and sidebar shell.
- **Surface Elevation 1 (`#16191a`):** Sidebar item backgrounds on hover, card container wells, and header bars.
- **Surface Elevation 2 (`#1e2223` / `#232729`):** Primary action cards, dialog modals, and flyouts.
- **Primary Accent (`#f59e0b` / Amber 500):** High-priority interactions, active indicator pills, notification counts, brand identity accents, and callout headings.
- **Secondary Accent (`#ea580c` / Orange 600):** Gradients, brand mark shading, and urgent badge counter backdrops.
- **Neutral Foregrounds:**
  - `High Contrast (#ffffff / #f8fafc)`: Primary headings, modal titles, and active menu labels.
  - `Mid Contrast (#94a3b8 / #71717a)`: Subtitles, helper text, and secondary navigation icons.
  - `Muted / Ghost (#40454a)`: Inactive arrows, structural dividers, and card borders.

## Typography

The typography uses **Inter** across all UI layers for consistency, clarity at dense information densities, and reliable cross-platform desktop rendering.

### Hierarchy & Style Rules
- **Display Headings (`headline-xl`, `headline-lg`):** Kept semi-bold (`600`) with tight tracking to maintain a clean editorial feel in greeting areas and page titles.
- **Card Headings (`headline-md`):** High contrast white text with compact line-height for immediate legibility.
- **Section Kickers & Caps (`label-md`):** Uppercase tracking (+0.06em) in muted slate (`#64748b`) or amber for category headers such as `MANAGE` or `OPTIONAL CLOUD UPGRADE`.
- **Secondary / Descriptor Copy (`body-md`, `body-sm`):** Low-contrast neutral gray (`#8a9099`) to establish clear hierarchy beneath crisp white titles.

## Layout & Spacing

The layout is built around a hybrid fixed-fluid desktop window architecture:
- **Sidebar Shell:** Fixed width of 240px–260px anchored to the left.
- **Content Area:** Fluid 3-column primary grid for dashboard action cards, switching to full-span single and double column panels for promotional banners and scheduled tasks.
- **Window Chrome:** Compact 40px top bar supporting custom desktop title bar dragging, branding, and standard window control affordances (minimize, maximize, close).

### Breakpoints & Adaptations
- **Standard Desktop (>1100px):** 3-column card arrangement; side-by-side secondary callouts.
- **Compact Desktop / Tablet (768px – 1099px):** 2-column card grid; sidebar collapsible into an icon rail (64px width).
- **Small Window (<768px):** 1-column stacked cards; drawer-based navigation.

## Elevation & Depth

Visual hierarchy is maintained through subtle tonal contrast and hairline borders rather than heavy drop shadows.

### Elevation Levels
- **Canvas (Level 0):** Pure deep charcoal `#0e1111` with no stroke.
- **Card Well (Level 1):** `#16191b` with a 1px border of `rgba(255, 255, 255, 0.04)`.
- **Raised Tiles (Level 2):** `#1d2123` with a 1px border of `rgba(255, 255, 255, 0.07)`. Soft ambient glow on hover (`0 8px 24px -4px rgba(0, 0, 0, 0.4)`).
- **Active Navigation Pill / Highlight (Level 3):** Outlined or glowing borders using `rgba(245, 158, 11, 0.4)` with an ambient amber bloom.

## Shapes

The design uses soft, rounded geometries that echo modern desktop window styling:
- **Action Cards & Content Panels:** 16px (`rounded-xl` to `rounded-2xl`) for a friendly yet structured feel.
- **Navigation Indicators & Pill Badges:** Fully circular / pill (`rounded-full`) for notification badges and active sidebar markers.
- **Buttons & Form Fields:** 8px to 10px (`rounded-lg`) for a clean, accessible click target.

## Components

### Buttons
- **Primary:** Solid warm amber (`#f59e0b`) background, dark charcoal text (`#121415`, weight `600`), 8px border-radius, with a subtle amber hover glow.
- **Secondary / Outline:** Transparent fill, `1px solid rgba(255, 255, 255, 0.12)`, white text, shifting to `border-amber-500/50` on focus or hover.
- **Ghost:** Minimal padding, subtle hover surface (`rgba(255, 255, 255, 0.05)`), used for window controls and top-right card arrows.

### Navigation List & Items
- **Active State:** Darkened surface pill (`#1d2123`), amber left indicator bar (3px width, full border radius), high-contrast amber or white label, and warm accent icon.
- **Inactive State:** Low-contrast icon and label (`#8e959e`), no border, transparent background, highlighting to muted gray-white on hover.
- **Category Labels:** Uppercase, tracking +0.06em, muted slate `#64748b` font.

### Cards & Action Tiles
- Large vertical cards with an icon box at top left, diagonal launch arrow at top right, bold white headline centered or bottom-anchored, followed by secondary muted body text.
- Footer indicator row specifying tool counts (e.g., "9 ways to make room") in low-contrast typography.

### Badges & Counters
- Compact circular or pill capsules (`#ea580c` or `#f59e0b`), bold dark or white numeric font (`label-sm`), placed adjacent to section labels to indicate pending actions.

### Inputs & Toggles
- Recessed input containers (`#141718`), 1px `border-white/10`, focusing with an amber outline (`#f59e0b`).