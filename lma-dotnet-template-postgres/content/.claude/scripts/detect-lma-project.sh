#!/bin/bash
# detect-lma-project.sh
# Detecta se o projeto atual é LMA pela estrutura de pastas e CLAUDE.md.
# Roda silenciosamente — exit 0 em todos os casos.

set -e
[ -z "${CLAUDE_PROJECT_DIR:-}" ] && exit 0

PROJECT_DIR="$CLAUDE_PROJECT_DIR"
IS_LMA=false

# Check 1: CLAUDE.md menciona LMA
if [ -f "$PROJECT_DIR/CLAUDE.md" ]; then
    grep -qE "LMA|Lean Modular Architecture" "$PROJECT_DIR/CLAUDE.md" 2>/dev/null && IS_LMA=true
fi

# Check 2: docs de arquitetura LMA
if [ "$IS_LMA" = false ]; then
    ls "$PROJECT_DIR"/docs/architecture/lma-*.md 2>/dev/null | grep -q . && IS_LMA=true
fi

# Check 3: estrutura Domain + Api + Modulos + Endpoints
if [ "$IS_LMA" = false ]; then
    HAS_DOMAIN=$(find "$PROJECT_DIR" -name "*.Domain.csproj" 2>/dev/null | head -1)
    HAS_API=$(find "$PROJECT_DIR" -name "*.Api.csproj" 2>/dev/null | head -1)
    HAS_MODULOS=$(find "$PROJECT_DIR" -type d -name "Modulos" 2>/dev/null | head -1)
    HAS_ENDPOINTS=$(find "$PROJECT_DIR" -type d -name "Endpoints" 2>/dev/null | head -1)

    [ -n "$HAS_DOMAIN" ] && [ -n "$HAS_API" ] && \
    [ -n "$HAS_MODULOS" ] && [ -n "$HAS_ENDPOINTS" ] && IS_LMA=true
fi

# Por ora, saída silenciosa.
# Expansão futura: emitir contexto adicional se IS_LMA=true e CLAUDE.md ausente.
exit 0
