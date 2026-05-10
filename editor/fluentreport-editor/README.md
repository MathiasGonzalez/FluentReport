# FluentReport Editor

Simple web editor for authoring reports with the FluentReport canonical schema.

## Current capabilities

- Create and reorder visual blocks: `text`, `line`, `spacer`, `pageBreak`, `image`, `table`, and `repeat`.
- Show a page preview for fast visual editing.
- Generate YAML from the canonical editor schema.
- Copy or download the generated `.frpt.yaml` file.
- Switch the editor UI between English and Spanish.

## Run in development

1. Install dependencies

   ```shell
   npm install
   ```

2. Start the app

   ```shell
   npm run dev
   ```

3. Build the project

   ```shell
   npm run build
   ```

## Publish to GitHub Pages

The editor is deployed with the workflow in [../../.github/workflows/editor-pages.yml](../../.github/workflows/editor-pages.yml).

- Trigger: push to `main` affecting `.github/workflows/editor-pages.yml` or `editor/fluentreport-editor/**`
- Manual deploy: `workflow_dispatch`
- Output: `editor/fluentreport-editor/dist`
- Pages requirement: in the repository settings, set GitHub Pages to deploy from `GitHub Actions`

The Vite `base` path is set automatically in CI, so the app is served correctly from the repository subpath on GitHub Pages.

## Generated format

The app emits the canonical authoring structure documented in [../../docs/schema/editor-yaml-schema.md](../../docs/schema/editor-yaml-schema.md):

- `kind: FluentReport`
- `schemaVersion: 1`
- `pageDefaults`
- `parameters`
- `dataSources`
- `styles`
- `rendererOptions`
- `definitions`
- `pages[].regions.*.nodes`

## Recommended next improvements

1. Full table authoring with richer column and row editing.
2. Editable header and footer areas directly from the UI.
3. Client-side schema validation with node-level messages.
4. Integration with the .NET backend for real PDF and HTML preview.
