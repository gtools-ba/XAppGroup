# User order (2026-11-21) — Header, card, and footer simplification

**Status:** Complete — all 4 items done (2026-11-21).
**Order 04:** Not started, as required.
**Rule:** Do not start the next item until the current one is built, tested, and reported.

**Decisions (2026-11-21):** Donate/GitHub are icon buttons only (tooltips, no
click action yet). Item 2 removes both the dashed `+ Add App` pill and every app
pill from cards. App-name visibility stays a per-group option in Edit Group and
applies to the popup labels only. Footer collapses to icons at the app's minimum
window width (**720 DIP**).

This list amends the in-review order 03 card work. Order 04 must not start until
this list is complete.

---

## To-do list

- [x] **1. Header (right side only).** Keep the header as it is. Remove the
      right-side minimize and maximize/restore caption buttons. Replace them with
      a **Donate** icon button and a **GitHub** icon button. Keep the existing
      refresh/settings buttons, search, brand, and the left traffic-light
      controls unchanged.
      - Verified 2026-11-21: build 0/0; self-test 45 PASS / 0 FAIL with
        `HEADER_ACTIONS`; live UIA showed `HeaderDonateBtn` (35×35 px) and
        `HeaderGitHubBtn` (35×35 px), both Invoke=True, and no
        `Minimize Window` / `Maximize / Restore Window` controls.

- [x] **2. Card actions — remove four controls.** On home-screen group cards
      remove: (a) the **open group launcher** button, (b) the **more options**
      button/menu, (c) the **"+ Add App"** dashed pill, and (d) **all app pills**
      (app shortcut icons and names). Keep the group icon (click = launcher,
      drag = pin) and the primary Launch/Add apps action.
      - Verified 2026-11-21: build 0/0; self-test 43 PASS / 0 FAIL with
        `CARD_ACTIONS ... RemovedButtons=0` and `EMPTY_CARD ...` (the primary
        Add-apps action remains). Live UIA dump showed only the group icon and
        Launch/Add-apps buttons on 14 cards; zero `More options`, launcher-only,
        app-pill, or Add-App-pill buttons; right-click opened no popup menu;
        ListItem heights were identical before and after selection change.

- [x] **3. Optional app names in the popup.** Keep the per-group
      **"Show app labels in popup"** checkbox in Edit Group; verify it hides and
      shows app names in the launcher popup and persists with the group. No new
      card-level checkbox is needed because cards no longer show app names.
      - Verified 2026-11-21: build 0/0; self-test 44 PASS / 0 FAIL with
        `POPUP_LABELS Checkbox=True LabelsOn=2 LabelsOff=0 ImagesOn=2
        ImagesOff=2 Persisted=True`. Live UIA: checkbox started On, toggling it
        Off + Save removed all app names from the launcher popup; toggling On +
        Save restored them; the saved JSON ends with `ShowLabels: true`.

- [x] **4. Responsive footer commands.** At the app's minimum window width
      (**720 DIP**) show **Edit**, **Duplicate**, **Create Shortcut**, and
      **Delete** as icons only; on wider windows keep the current labeled
      buttons. New remains labeled throughout.
      - Verified 2026-11-21: build 0/0; self-test 45 PASS / 0 FAIL with
        `FOOTER_RESPONSIVE WideLabeled=True NarrowIcons=True NewLabeled=True
        NarrowWidths='31,31,31,31' ... Restored=True`. Live UIA at 900 px
        (720 DIP): the four commands were 39 px with no text children, with
        Names/HelpTexts `Edit`, `Duplicate`, `Create Shortcut`, `Delete`; New
        stayed 138 px and labeled. At 1120 px (896 DIP) all four labels returned.
