# Follow-up order (2026-11-21) — Card app icons, app count, footer breakpoint

**Status:** Complete.
**Rule:** Do not start the next item until the current one is built, tested, and reported.

This amends the just-completed header/card/footer simplification. Answers given
by the user before starting:

- Cards show app **icons only**, no app names; the dashed `+ Add App` pill stays
  removed, as do the launcher and more-options buttons.
- The visible app count is a **fixed 8**; hidden apps are summarized as `+N`.
- Footer icons-only mode applies at **720 DIP and 896 DIP**; labels return only
  above 896 DIP.

---

## To-do list

- [x] **1. Restore icon-only app buttons on home cards.** Re-add the per-app
      icon buttons (no name text) and the hidden-count badge. Show the first 8
      apps and `+N` for the rest. Keep the primary Launch/Add-apps action and
      keep the group icon (click = launcher, drag = pin). Accessible names and
      tooltips may still carry the app name.
      - Verified 2026-11-21: build 0/0; self-test 46 PASS / 0 FAIL with
        `CARD_OVERFLOW DataPills=8 DataOverflow='+4' UIPills=8 UIBadge=1
        EmptyPills=0` and `CARD_ACTIONS ... AppIcons=2 IconsOnly=True`.
        Live UIA: 12-app groups show exactly 8 `Launch <app>` buttons with no
        text children and a `+4` badge; badges `+1`, `+3`, `+8`, `+22` match
        the other group sizes.

- [x] **2. Extend footer icons-only mode to 896 DIP.** Keep Edit / Duplicate /
      Create Shortcut / Delete icon-only at 720 DIP and at the default
      896 DIP; show labels only when the window is wider than 896 DIP. New
      stays labeled throughout.
      - Verified 2026-11-21: build 0/0; self-test 46 PASS / 0 FAIL with
        `INFO FOOTER_RESPONSIVE Icons720=True Widths720='31,31,31,31'
        Icons896=True Widths896='31,31,31,31' Labeled1024=True
        Restored896=True Names='Edit,Duplicate,Create Shortcut,Delete'`.
        Live UIA: 900 px and 1120 px show four 39 px icon-only commands with
        Names/HelpTexts intact and New labeled; 1280 px shows labels;
        returning to 1120 px restores icons.
