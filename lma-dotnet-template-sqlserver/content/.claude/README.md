# Configuração Claude Code — LMA

> Skills, commands, subagent e hooks para projetos seguindo **Lean Modular Architecture (LMA) v1.0** — arquitetura modular para APIs .NET otimizada para produtividade humana e de IA.

## O que é LMA

Dois projetos físicos, módulos de negócio, Ações como vertical slices, acesso direto a dados. Princípio fundador:

> **Separe por módulo de negócio e por ação. Isole o que muda de verdade. Otimize para que a estrutura seja previsível e gerável.**

## Instalação

Nenhuma. Esta pasta já está na raiz do seu projeto — o Claude Code carrega tudo
automaticamente ao abrir o diretório.

Na primeira execução o Claude Code vai pedir aprovação dos hooks declarados em
`.claude/settings.json`. Use `/hooks` para revisar.

## O que tem aqui

### 3 Skills — `skills/`

Ativam sozinhas pelo contexto da conversa.

| Skill | Dispara quando |
|---|---|
| `lma-create-action` | "criar ação", "novo endpoint", "criar caso de uso" |
| `lma-refactor-to-rich` | "tornar entidade rica", "encapsular regras", "mover regra para Domain" |
| `lma-add-module` | "criar módulo", "adicionar bounded context", "criar área de X" |

### 3 Commands — `commands/`

| Command | O que faz |
|---|---|
| `/lma-init <projeto> [--database]` | Cria projeto .NET com dois csproj no padrão LMA |
| `/lma-review [--staged\|--last-commit\|<caminho>]` | Checklist de PR nos arquivos alterados |
| `/lma-docs [principios\|estrutura\|templates\|antipatterns\|all]` | Referência rápida |

### 1 Subagent — `agents/`

- **`lma-reviewer`** — Revisão arquitetural profunda, com análise contextual, precisão de linha, antes/depois.

### 2 Hooks — `settings.json` + `scripts/`

- **`validate-lma-conventions.sh`** (PostToolUse) — Valida 8 convenções LMA após cada Write/Edit em `.cs` e devolve as violações para o Claude corrigir.
- **`detect-lma-project.sh`** (UserPromptSubmit) — Detecta se o projeto segue LMA pela estrutura de pastas.

Ambos são bash. No Windows, exigem Git Bash ou WSL no `PATH`.

### Docs — `docs/`

- `lma-v1.0.md` — Documento arquitetural completo (20 seções)
- `lma-templates.md` — 5 templates de Ação copiáveis
- `lma-checklist-pr.md` — Checklist de PR em 17 seções
- `CLAUDE-template.md` — CLAUDE.md enxuto pronto para o root do projeto

## Uso rápido

```
# Criar primeiro módulo
"criar módulo de Clientes"          → skill lma-add-module

# Criar Ação
"criar ação para buscar produtos"   → skill lma-create-action

# Revisar antes do merge
/lma-review --staged
```

---

**LMA version:** 1.0 | **Compatibilidade:** Claude Code 2.1+
**Licença:** MIT — Leanwork Group
