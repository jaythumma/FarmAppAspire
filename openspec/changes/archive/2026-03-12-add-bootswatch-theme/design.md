## Context

`FarmAppAspire.Web` ships Bootstrap 5 as a vendored local file (`wwwroot/lib/bootstrap/bootstrap.min.css`) referenced from `App.razor`. The default Bootstrap theme is visually plain and requires manual colour overrides in `app.css` to achieve branded styling. Bootswatch provides polished, drop-in Bootstrap-compatible themes hosted on jsDelivr CDN — swapping the CSS link is the entire change.

The Blazor Web project uses .NET 10 static asset fingerprinting (`@Assets["..."]`) for local files. CDN links bypass fingerprinting, which is acceptable for a third-party stylesheet that is versioned by URL.

## Goals / Non-Goals

**Goals:**
- Replace the local Bootstrap CSS with the Bootswatch **Flatly** theme via jsDelivr CDN.
- Remove colour/button overrides from `app.css` that Flatly now owns.
- Align `NavMenu.razor` navbar classes with Flatly's palette.
- Keep the vendored Bootstrap JS bundle (popper + bootstrap.bundle.min.js) — only the CSS changes.

**Non-Goals:**
- Switching away from Bootstrap (all classes remain Bootstrap-compatible).
- Adding a theme picker or runtime theme switching.
- Changing any component behaviour, API, or data model.
- Theming other projects in the solution.

## Decisions

### D1 — CDN over local vendored file
**Decision:** Reference Bootswatch Flatly from `https://cdn.jsdelivr.net/npm/bootswatch@5/dist/flatly/bootstrap.min.css` rather than copying the file to `wwwroot/lib/`.

**Rationale:** Bootswatch is not in the default LibMan/npm registry used by this project. A CDN link requires zero tooling, matches how Bootswatch is documented, and pins the version by URL. The JS bundle is untouched so Bootstrap interactivity (dropdowns, modals) is unaffected.

**Alternative considered:** Download and vendor the file locally. Rejected — adds a maintenance step and the project has no existing npm/LibMan workflow to automate it.

### D2 — Flatly as the chosen theme
**Decision:** Use **Flatly** (flat, professional, green/teal accent).

**Rationale:** Flatly's colour palette suits an agricultural management application. It provides strong contrast, clear typography, and a clean table style — all needed by the customer list and admin pages. Other candidates considered: Lumen (too light), Cosmo (too blue), Sandstone (warm but lower contrast).

**Alternative considered:** Sketchy, Morph. Rejected as too decorative for a business app.

### D3 — Targeted `app.css` pruning
**Decision:** Remove only the rules in `app.css` that directly conflict with Flatly (`.btn-primary` colour block, `a` colour, focus `box-shadow`). Retain layout rules (`.sidebar`, `.content`, `.top-row`), validation colours, and Blazor error UI styles.

**Rationale:** Minimises scope and diff size. Over-pruning risks breaking layout; under-pruning risks visual conflicts.

## Risks / Trade-offs

- **CDN availability** → Mitigation: jsDelivr has 99.9%+ uptime SLA; the app is already CDN-dependent for nothing critical at this layer. Could fall back to local file if offline deployment is required in future.
- **Future Bootstrap version upgrades** → Mitigation: The CDN URL includes the major version (`bootswatch@5`); a minor version bump to Bootstrap JS does not change the CSS API.
- **Flatly overrides more than expected** → Mitigation: Run the app visually after each changed file; the change is fully reversible (one link swap).
- **NavMenu class change breaks layout on mobile** → Mitigation: Test responsive layout on narrow viewport; the sidebar layout in `app.css` is independent of navbar theme classes.

## Migration Plan

1. Swap the `<link>` in `App.razor` (one line change).
2. Prune `app.css` conflicting rules.
3. Update `NavMenu.razor` navbar classes.
4. Run the app and visually verify all pages (home, customers list, customer detail, admin/users, login).
5. Rollback: revert the `<link>` to `@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]` and restore removed `app.css` rules.

## Open Questions

*(none — scope is small and fully reversible)*
