#!/bin/bash
# validate-lma-conventions.sh
# Roda após Write ou Edit em arquivos .cs — alerta sobre violações LMA via stderr.
# Sai com 2 quando encontra violação: no PostToolUse isso não desfaz a escrita,
# apenas devolve o stderr para o Claude ler e corrigir. Sem violação, exit 0.

set -e
INPUT=$(cat)

if command -v jq &> /dev/null; then
    FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // .tool_input.path // empty')
else
    FILE_PATH=$(echo "$INPUT" | sed -n 's/.*"file_path":[[:space:]]*"\([^"]*\)".*/\1/p')
    [ -z "$FILE_PATH" ] && FILE_PATH=$(echo "$INPUT" | sed -n 's/.*"path":[[:space:]]*"\([^"]*\)".*/\1/p')
fi

[[ ! "$FILE_PATH" =~ \.cs$ ]] && exit 0
[ ! -f "$FILE_PATH" ] && exit 0

WARNINGS=""

# ─── 1: Domain não pode importar infra ──────────────────────────────────
if [[ "$FILE_PATH" =~ /Domain/ ]]; then
    if grep -qE "using Microsoft\.EntityFrameworkCore|using MediatR|using Microsoft\.AspNetCore" "$FILE_PATH"; then
        WARNINGS+="🔥 [LMA] $FILE_PATH está em Domain e importa framework de infra.\n"
        WARNINGS+="   Domain deve ser puro. Remova o using ou mova para MinhaApp.Api.\n\n"
    fi
fi

# ─── 2: Ação não pode ter sufixo técnico ────────────────────────────────
BASENAME=$(basename "$FILE_PATH" .cs)
if [[ "$FILE_PATH" =~ /Modulos/ ]] && \
   [[ ! "$FILE_PATH" =~ (Request|Response|Validator)\.cs$ ]]; then
    if echo "$BASENAME" | grep -qE "(Handler|Service|UseCase|Command|Query|Reader|Writer)$"; then
        WARNINGS+="🔥 [LMA] Nome '$BASENAME' usa sufixo técnico proibido.\n"
        WARNINGS+="   Use o verbo de negócio sem sufixo (ex: ProcessarPedido, BuscarProdutos).\n\n"
    fi
fi

# ─── 3: Ação não pode referenciar HttpContext / ClaimsPrincipal ──────────
if [[ "$FILE_PATH" =~ /Modulos/ ]] && \
   [[ ! "$FILE_PATH" =~ (Request|Response|Validator)\.cs$ ]]; then
    if grep -qE "IHttpContextAccessor|ClaimsPrincipal|HttpContext" "$FILE_PATH"; then
        WARNINGS+="🔥 [LMA] $FILE_PATH (Ação) acessa protocolo web.\n"
        WARNINGS+="   JWT/Claims devem ser extraídos no Endpoint e passados limpos no Request.\n\n"
    fi
fi

# ─── 4: Ação não pode referenciar Repository ────────────────────────────
if [[ "$FILE_PATH" =~ /Modulos/ ]] && \
   [[ ! "$FILE_PATH" =~ (Request|Response|Validator)\.cs$ ]]; then
    if grep -qE "IRepository|I[A-Z][a-zA-Z]+Repository|I[A-Z][a-zA-Z]+Writer" "$FILE_PATH"; then
        WARNINGS+="🔥 [LMA] $FILE_PATH (Ação) injeta Repository ou Writer.\n"
        WARNINGS+="   LMA injeta AppDbContext ou IDbConnectionFactory diretamente. Sem Repository.\n\n"
    fi
fi

# ─── 5: Ação não pode declarar rota ─────────────────────────────────────
if [[ "$FILE_PATH" =~ /Modulos/ ]] && \
   [[ ! "$FILE_PATH" =~ (Request|Response|Validator|Endpoints)\.cs$ ]]; then
    if grep -qE "MapGet|MapPost|MapPut|MapDelete|MapPatch|IEndpointRouteBuilder" "$FILE_PATH"; then
        WARNINGS+="🔥 [LMA] $FILE_PATH (Ação) declara rota ou conhece HTTP.\n"
        WARNINGS+="   Roteamento centralizado em Endpoints/{Modulo}Endpoints.cs.\n\n"
    fi
fi

# ─── 6: Ação de leitura não pode chamar SaveChangesAsync ─────────────────
if [[ "$FILE_PATH" =~ /Modulos/ ]] && \
   [[ ! "$FILE_PATH" =~ (Request|Response|Validator)\.cs$ ]]; then
    if grep -q "AsNoTracking" "$FILE_PATH" && grep -q "SaveChangesAsync" "$FILE_PATH"; then
        WARNINGS+="🔥 [LMA] $FILE_PATH parece ser leitura (AsNoTracking) mas chama SaveChangesAsync.\n"
        WARNINGS+="   Ação de leitura nunca muda estado. Remova SaveChangesAsync ou separe em outra Ação.\n\n"
    fi
fi

# ─── 7: MediatR ou AutoMapper proibidos ─────────────────────────────────
if grep -qE "using MediatR|IRequest<|IRequestHandler<" "$FILE_PATH"; then
    WARNINGS+="🔥 [LMA] $FILE_PATH usa MediatR — proibido em LMA.\n"
    WARNINGS+="   Ações são classes normais injetadas via DI. Sem MediatR.\n\n"
fi

if grep -qE "using AutoMapper|IMapper|_mapper\.Map\b" "$FILE_PATH"; then
    WARNINGS+="🔥 [LMA] $FILE_PATH usa AutoMapper — proibido em LMA.\n"
    WARNINGS+="   Use mapeamento explícito (3 linhas, zero mágica).\n\n"
fi

# ─── 8: Execute deve existir em Ações ───────────────────────────────────
if [[ "$FILE_PATH" =~ /Modulos/ ]] && \
   [[ ! "$FILE_PATH" =~ (Request|Response|Validator)\.cs$ ]]; then
    if ! grep -qE "Task.*Execute[[:space:]]*\(|async[[:space:]]+Task.*Execute" "$FILE_PATH"; then
        WARNINGS+="⚠️  [LMA] $FILE_PATH não tem método público 'Execute'.\n"
        WARNINGS+="   Convenção LMA: método público único de toda Ação chama-se Execute.\n\n"
    fi
fi

# ─── Output ──────────────────────────────────────────────────────────────
if [ -n "$WARNINGS" ]; then
    echo "═══════════════════════════════════════════════════════════════" >&2
    echo "  LMA — Convenções verificadas em: $(basename "$FILE_PATH")" >&2
    echo "═══════════════════════════════════════════════════════════════" >&2
    echo -e "$WARNINGS" >&2
    echo "Revisão completa: /lma-review ou invoke subagent 'lma-reviewer'" >&2
    echo "═══════════════════════════════════════════════════════════════" >&2
    exit 2
fi

exit 0
