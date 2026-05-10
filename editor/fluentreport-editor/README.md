# FluentReport Editor

Editor web simple para crear reportes en formato declarativo FluentReport Schema v1.

## Que hace

- Permite crear y ordenar bloques visuales: text, line, spacer, pageBreak.
- Muestra una vista previa de pagina para edicion rapida.
- Genera YAML del esquema v1.
- Permite copiar o descargar el archivo generado con extension frpt.yaml.

## Ejecutar en desarrollo

1. Instalar dependencias

   npm install

2. Levantar la app

   npm run dev

3. Compilar

   npm run build

## Formato generado

La app genera estructura base compatible con la especificacion propuesta en el repositorio:

- kind: FluentReport
- schemaVersion: 1
- pageDefaults
- parameters
- styles
- pages[].content.items

## Proximas mejoras recomendadas

1. Soporte de tabla con rows.source y cells.
2. Soporte de header y footer editables desde UI.
3. Validacion de schema en cliente con mensajes por nodo.
4. Integracion con backend .NET para preview real PDF/HTML.
