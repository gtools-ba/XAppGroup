---
name: Fluent Amber Mica
colors:
  surface: '#f9f9ff'
  surface-dim: '#d3daef'
  surface-bright: '#f9f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f1f3ff'
  surface-container: '#e9edff'
  surface-container-high: '#e1e8fd'
  surface-container-highest: '#dce2f7'
  on-surface: '#141b2b'
  on-surface-variant: '#554336'
  inverse-surface: '#293040'
  inverse-on-surface: '#edf0ff'
  outline: '#887364'
  outline-variant: '#dbc2b0'
  surface-tint: '#904d00'
  primary: '#8d4b00'
  on-primary: '#ffffff'
  primary-container: '#b15f00'
  on-primary-container: '#fffbff'
  inverse-primary: '#ffb77d'
  secondary: '#855300'
  on-secondary: '#ffffff'
  secondary-container: '#fea619'
  on-secondary-container: '#684000'
  tertiary: '#984300'
  on-tertiary: '#ffffff'
  tertiary-container: '#bb5810'
  on-tertiary-container: '#fffbff'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#ffdcc3'
  primary-fixed-dim: '#ffb77d'
  on-primary-fixed: '#2f1500'
  on-primary-fixed-variant: '#6e3900'
  secondary-fixed: '#ffddb8'
  secondary-fixed-dim: '#ffb95f'
  on-secondary-fixed: '#2a1700'
  on-secondary-fixed-variant: '#653e00'
  tertiary-fixed: '#ffdbca'
  tertiary-fixed-dim: '#ffb68e'
  on-tertiary-fixed: '#331200'
  on-tertiary-fixed-variant: '#763300'
  background: '#f9f9ff'
  on-background: '#141b2b'
  surface-variant: '#dce2f7'
typography:
  headline-xl:
    fontFamily: Segoe UI Variable
    fontSize: 40px
    fontWeight: '600'
    lineHeight: 52px
    letterSpacing: -0.02em
  headline-xl-mobile:
    fontFamily: Segoe UI Variable
    fontSize: 30px
    fontWeight: '600'
    lineHeight: 38px
    letterSpacing: -0.015em
  headline-lg:
    fontFamily: Segoe UI Variable
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
    letterSpacing: -0.015em
  headline-lg-mobile:
    fontFamily: Segoe UI Variable
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Segoe UI Variable
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
    letterSpacing: -0.01em
  body-lg:
    fontFamily: Segoe UI Variable
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
    letterSpacing: 0em
  body-md:
    fontFamily: Segoe UI Variable
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
    letterSpacing: 0em
  body-sm:
    fontFamily: Segoe UI Variable
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
    letterSpacing: 0.01em
  label-md:
    fontFamily: Segoe UI Variable
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
    letterSpacing: 0.005em
  label-sm:
    fontFamily: Segoe UI Variable
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.01em
  caption:
    fontFamily: Segoe UI Variable
    fontSize: 11px
    fontWeight: '400'
    lineHeight: 14px
    letterSpacing: 0.02em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  gutter: 1rem
  gutter-desktop: 1.5rem
  margin: 1rem
  margin-desktop: 2rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 0.75rem
  space-lg: 1.25rem
  space-xl: 2rem
---

## Brand & Style

This design system channels Windows 11’s Fluent Design philosophy, specifically the interplay between tactile physical materials—Mica, Acrylic, and subtle frosted smoke—and precision digital utilities. The aesthetic balances luminous optical materials with crisp, high-density corporate productivity. 

The emotional target is architectural clarity: quiet, daylight-washed surfaces that elevate foreground content without visual fatigue. The signature visual metaphor relies on optical depth, where background desktop tones subtly tint multi-layered translucent white plates, rimmed with razor-thin specular highlights and balanced by rich amber gold interactive accents.

## Colors

The palette revolves around daylight transmission and rich optical balance:

- **Primary (`#d97706`) & Secondary Accent (`#f59e0b`):** Deep amber and warm gold. Used intentionally for focal action points, selection borders, active toggle pills, and high-visibility state indicators.
- **Tertiary Accent (`#b45309`):** Burnished amber for active press states, link hovers, and high-contrast focus rings.
- **Surface Systems (Mica & Acrylic Glass):**
  - **Base Canvas:** `#f8fafc` overlaid with an ambient warm wallpaper glow.
  - **Mica Sheet:** `rgba(255, 255, 255, 0.72)` with `backdrop-filter: blur(20px) saturate(160%)`.
  - **Acrylic Layer (Flyouts, Menus, Modals):** `rgba(255, 255, 255, 0.85)` with `backdrop-filter: blur(32px) saturate(180%)`.
  - **Subtle Inset Well:** `rgba(0, 0, 0, 0.03)` with `rgba(0, 0, 0, 0.04)` interior stroke.
- **Text & Foreground:**
  - **Primary Text:** `#111827` (Deep neutral charcoal, avoiding pure `#000000` to preserve optical harmony).
  - **Secondary Text:** `#374151` (Mid-tone graphite for subheaders, labels, and secondary indicators).
  - **Disabled / Tertiary Text:** `#9ca3af`.
- **Specular Borders:** Dual-stroke methodology utilizing a baseline outline of `rgba(0, 0, 0, 0.08)` grounded with an inset pseudo-specular top rim of `rgba(255, 255, 255, 0.80)`.

## Typography

Typography is governed by `Segoe UI Variable` (with fallbacks to `Inter` and system sans-serif engines). The type hierarchy emphasizes vertical density and baseline balance suited for multi-window, dense administrative productivity. 

Use standard proportional spacing for general reading and body contexts, but shift to tight tabular figures (`font-variant-numeric: tabular-nums`) for telemetry, numeric metrics, and data tables. Maintain tight line heights on titles to echo fluent window titles, while preserving a comfortable 1.5x ratio on standard body blocks to safeguard legibility against translucent backdrop layers.

## Layout & Spacing

This layout follows a strict 4px/8px modular base scale operating inside a fluid 12-column responsive grid:

- **Desktop (1024px+):** Fluid content grid flanked by an optional static acrylic sidebar (width 260px–320px). Exterior margins standard at `2rem`, column gutters set to `1.5rem`.
- **Tablet (641px–1023px):** 8-column layout. Exterior margins reduce to `1.5rem`, gutters consolidate to `1rem`. Secondary sidebars collapse into overlay acrylic sheets.
- **Mobile (<=640px):** 4-column layout. Exterior margins standard at `1rem`, gutters at `0.75rem`. Controls maximize horizontal hit areas while maintaining compact vertical heights (minimum touch target: 40px with 4px internal visual padding).

Spacing between card modules adheres to `space-md` (12px) or `space-lg` (20px) to simulate grouped Windows 11 app tiles and settings categories.

## Elevation & Depth

Visual hierarchy uses physical material layering and directional specular lighting rather than heavy drop shadows:

- **Level 0 (App Canvas / Desktop):** Warm neutral base (`#f8fafc`) receiving subtle wallpaper absorption.
- **Level 1 (Mica Surfaces / Primary Work Panels):** 
  - Fill: `rgba(255, 255, 255, 0.72)`
  - Filter: `backdrop-filter: blur(20px) saturate(160%)`
  - Border: `1px solid rgba(0, 0, 0, 0.08)`
  - Shadow: `0 1px 3px rgba(0, 0, 0, 0.04), 0 2px 8px rgba(0, 0, 0, 0.02)`
  - Specular Highlight: Inset box-shadow `inset 0 1px 0 rgba(255, 255, 255, 0.8)`
- **Level 2 (Active Cards / Floating Panels):** 
  - Fill: `rgba(255, 255, 255, 0.82)`
  - Filter: `backdrop-filter: blur(24px) saturate(170%)`
  - Shadow: `0 4px 12px rgba(0, 0, 0, 0.06), 0 1px 2px rgba(0, 0, 0, 0.04)`
  - Specular Highlight: Inset box-shadow `inset 0 1px 0 rgba(255, 255, 255, 0.9)`
- **Level 3 (Acrylic Flyouts / Context Menus / Popovers):**
  - Fill: `rgba(255, 255, 255, 0.88)`
  - Filter: `backdrop-filter: blur(32px) saturate(180%)`
  - Shadow: `0 12px 32px rgba(0, 0, 0, 0.12), 0 4px 8px rgba(0, 0, 0, 0.04)`
  - Border: `1px solid rgba(0, 0, 0, 0.09)`
  - Specular Highlight: Inset box-shadow `inset 0 1px 0 rgba(255, 255, 255, 1.0)`

## Shapes

The geometric architecture uses `roundedness: 2`, establishing an 8px (`0.5rem`) corner radius standard for cards, text fields, lists, and context flyouts. 

Exceptions are explicitly managed:
- **Pill Controls:** Segmented switchers, active category filters, badges, and toggle tracks utilize a fully round pill radius (`9999px`).
- **Inner Nested Elements:** When a nested control is placed within a card with 8px radius, the child radius scales to 4px to maintain concentric geometric padding.
- **Window Shells / Main Dialogs:** Outer container envelopes scale to 12px or 16px (`rounded-xl`).

## Components

### Buttons
- **Primary:** Warm amber fill (`#d97706`), text in `#ffffff`, subtle top specular line `inset 0 1px 0 rgba(255, 255, 255, 0.25)`. Hover transitions to `#b45309` with elevation lift. Active state compresses scale to `0.985`.
- **Standard (Acrylic Glass Pill):** Surface `rgba(255, 255, 255, 0.70)`, stroke `1px solid rgba(0, 0, 0, 0.08)`, text `#111827`, inset highlight `inset 0 1px 0 rgba(255, 255, 255, 0.90)`. Hover background shifts to `rgba(255, 255, 255, 0.92)`.

### Input Fields
- Grounded base container: Background `rgba(255, 255, 255, 0.65)`, backdrop blur `12px`, perimeter border `1px solid rgba(0, 0, 0, 0.08)`, bottom accent stroke `1px solid rgba(0, 0, 0, 0.25)`.
- Focused state: Bottom stroke expands to 2px solid `#d97706` using a smooth horizontal reveal, background lightens to `#ffffff`, and focus ring stays flush without coarse offsets.

### Cards & Settings Rows
- Master-detail rows: Border-radius 8px, background `rgba(255, 255, 255, 0.55)`, border `1px solid rgba(0, 0, 0, 0.06)`, padding `12px 16px`.
- Interactive items receive a `1px` subtle highlight and surface transformation to `rgba(255, 255, 255, 0.85)` on hover.

### Toggle Switches & Radio Buttons
- **Toggles:** Track width 40px, height 20px, rounded pill. Inactive track is `rgba(0, 0, 0, 0.12)`. Active track fills with `#d97706`. Thumb is pure white `#ffffff` with a crisp directional micro-shadow (`0 1px 3px rgba(0,0,0,0.20)`).
- **Checkboxes & Radios:** 18px dimension, rounded-sm (4px) for checkboxes and circular for radios. Resting border `1px solid rgba(0, 0, 0, 0.3)`. Checked state displays `#d97706` fill with an inset pure white glyph.

### Chips & Filter Tabs
- Frosted translucent capsules (`border-radius: 9999px`) with padding `4px 12px`. Unselected tabs use `rgba(0, 0, 0, 0.04)` fill and `#374151` label. Selected pill activates with white glass `rgba(255, 255, 255, 0.92)`, subtle drop shadow, and bold amber indicator bar or text.

### Flyouts & Command Bars
- Floating command bars utilize Acrylic styling (`backdrop-filter: blur(28px)`), rounded-xl margins, inner gap of 4px between mini-actions, and integrated micro-divider lines (`1px solid rgba(0, 0, 0, 0.06)`).