# Mundialito client (AngularJS)

This folder is the source for the web UI. The app is served from `wwwroot/` after a Gulp build.

## Edit workflow

1. Change JavaScript in `src/` and HTML templates in `src/**/*.html`.
2. Change styles in `css/site.css` (app utilities) or `css/mundialito-theme.css` (look and feel).
3. Run from the `Mundialito/` project folder:

```bash
npm install
npx gulp default
```

4. Do not edit hashed files under `wwwroot/js/` or `wwwroot/lib/` by hand.

## Themes

The server picks a theme from config (`App:Theme`):

- `cerulean` loads `wwwroot/css/app-cerulean.css`
- anything else loads `wwwroot/css/app-space-lab.css`

Both bundles include Bootswatch plus `mundialito-theme.css` at the end. The shell adds `theme-cerulean` or `theme-spacelab` on `<body>` so each theme has distinct colors (navbar, page background, panels).

## Vendor library versions (Client/lib)

- jQuery 3.7.1
- AngularJS 1.8.3 (angular, animate, sanitize, resource, route)
- ui-grid 4.12.7

## Styling rules

- Keep Bootstrap 3 class names in HTML (`panel`, `navbar`, `col-md-*`, etc.).
- Put visual overrides in `mundialito-theme.css` under `.mundialito-ui` (set on `<body>` in `Views/Home/Index.cshtml`).
- Do not add Bootstrap 4/5 or Tailwind without a full migration plan.

## Do not remove from the JS bundle without checking

- `autofields` — Register page uses `<auto:fields>`
- `ng-sortable` — key-value editor on admin screens
- `html2canvas` — user profile share feature
