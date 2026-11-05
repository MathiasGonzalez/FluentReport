#!/usr/bin/env bash
# Builds llms-full.txt: llms.txt followed by the user-facing docs, so an AI coding assistant can learn
# the whole API from one file. Run it after editing any of the sources below; CI runs it with --check
# and fails when the committed file is stale.
set -euo pipefail
cd "$(dirname "$0")/.."

BLOB="https://github.com/MathiasGonzalez/FluentReport/blob/main"

# Relative Markdown links would break once the file is served from GitHub Pages: make them absolute.
absolute_links() { # $1 = directory of the source file, relative to the repo root ("" for the root)
  perl -pe 's{\]\((?!https?:|#|mailto:)([^)]+)\)}{]('"$BLOB"'/'"${1:+$1/}"'$1)}g; s{/[^/()]+/\.\./}{/}g'
}

source_file() { # $1 = path
  printf '\n\n---\n\n<!-- Source: %s -->\n\n' "$1"
}

build() {
  cat llms.txt

  # README without the logo and badges, and without the sections about developing this repository.
  source_file README.md
  awk '
    /^!\[/ || /^\[!\[/ { next }
    /^## Project structure/ || /^## Releasing/ { skip = 1; next }
    /^## / { skip = 0 }
    !skip
  ' README.md | cat -s | absolute_links ""

  for doc in docs/api.md docs/rdlc-limitations.md SECURITY.md; do
    source_file "$doc"
    dir=$(dirname "$doc"); [ "$dir" = "." ] && dir=""
    absolute_links "$dir" < "$doc"
  done
}

if [ "${1:-}" = "--check" ]; then
  if ! build | diff -q - llms-full.txt > /dev/null; then
    echo "llms-full.txt is out of date: run scripts/build-llms-full.sh and commit the result." >&2
    exit 1
  fi
  echo "llms-full.txt is up to date."
else
  build > llms-full.txt
  echo "Wrote llms-full.txt ($(wc -c < llms-full.txt | tr -d ' ') bytes)."
fi
