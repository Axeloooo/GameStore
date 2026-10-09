#!/usr/bin/env bash
# Read-only pre-flight check before deploying GameStore to Azure with azd.
#
# It only inspects this machine and the repository: tool versions, Azure CLI login state,
# resource provider registration, user-secret names and key prefixes. It never creates or
# changes anything, and it never prints subscription names or IDs, tenant IDs, keys or secret values.
#
# Usage:
#   scripts/deploy-preflight.sh          # quick checks
#   scripts/deploy-preflight.sh --full   # also build the solution and run the unit tests
#
# Exit code: 0 when no check FAILED (warnings are allowed), 1 otherwise.

set -u

FULL=0
[ "${1:-}" = "--full" ] && FULL=1

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APPHOST="$ROOT/backend/src/GameStore.AppHost"

PASS=0
WARN=0
FAIL=0

pass() { PASS=$((PASS + 1)); printf '  [ OK ] %s\n' "$1"; }
warn() { WARN=$((WARN + 1)); printf '  [WARN] %s\n' "$1"; }
fail() { FAIL=$((FAIL + 1)); printf '  [FAIL] %s\n' "$1"; }
section() { printf '\n%s\n' "$1"; }

have() { command -v "$1" >/dev/null 2>&1; }

section "Tools"
if have dotnet; then
  v="$(dotnet --version 2>/dev/null)"
  case "$v" in
    8.*|9.*|10.*|11.*) pass "dotnet $v (projects target net8.0)" ;;
    *) fail "dotnet $v: .NET SDK 8 or newer is required" ;;
  esac
else
  fail "dotnet not found"
fi

if have docker && docker info >/dev/null 2>&1; then
  pass "docker daemon running ($(docker info --format '{{.ServerVersion}}' 2>/dev/null))"
elif have docker; then
  warn "docker installed but the daemon is not running (only needed for local runs and tests)"
else
  warn "docker not found (only needed for local runs and tests)"
fi

if have node; then
  nv="$(node --version 2>/dev/null)"
  major="${nv#v}"; major="${major%%.*}"
  if [ "${major:-0}" -ge 20 ] 2>/dev/null; then pass "node $nv"; else fail "node $nv: Node 20 or newer is required"; fi
else
  fail "node not found"
fi

if have az; then
  pass "az $(az version --query '"azure-cli"' -o tsv 2>/dev/null)"
else
  fail "az (Azure CLI) not found: brew install azure-cli"
fi

if have azd; then
  pass "azd $(azd version 2>/dev/null | head -1)"
else
  fail "azd (Azure Developer CLI) not found: see https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd"
fi

if have stripe; then
  pass "stripe CLI present (only needed for local runs)"
else
  warn "stripe CLI not found (only needed for local runs; the AppHost uses a container for it)"
fi

if have gh && gh auth status >/dev/null 2>&1; then
  pass "gh signed in"
else
  warn "gh not signed in (only needed to open pull requests)"
fi

section "Azure login"
LOGGED_IN=0
if have az; then
  if az account show >/dev/null 2>&1; then
    LOGGED_IN=1
    pass "az is signed in"
  else
    fail "az is not signed in: run 'az login --tenant <tenant-id>' then 'az account set --subscription <subscription-id>'"
  fi
  if az extension show --name containerapp >/dev/null 2>&1; then
    pass "az extension 'containerapp' installed"
  else
    warn "az extension 'containerapp' missing (runbook commands use it): az extension add --name containerapp"
  fi
fi
if have azd; then
  # 'azd auth login --check-status' exits 0 even when signed out, so read its message
  azd_status="$(azd auth login --check-status 2>&1)"
  if printf '%s' "$azd_status" | grep -qi "not logged in"; then
    fail "azd is not signed in: run 'azd auth login --tenant-id <tenant-id>'"
  elif printf '%s' "$azd_status" | grep -qi "logged in"; then
    pass "azd is signed in"
  else
    warn "could not determine azd login state: run 'azd auth login --check-status'"
  fi
fi

section "Azure resource providers (registration state only)"
if [ "$LOGGED_IN" -eq 1 ]; then
  for p in Microsoft.App Microsoft.ContainerRegistry Microsoft.DBforPostgreSQL Microsoft.Storage \
           Microsoft.ServiceBus Microsoft.KeyVault Microsoft.Cdn Microsoft.Insights Microsoft.OperationalInsights; do
    state="$(az provider show -n "$p" --query registrationState -o tsv 2>/dev/null)"
    if [ "$state" = "Registered" ]; then
      pass "$p registered"
    else
      warn "$p is '${state:-unknown}': az provider register -n $p"
    fi
  done
else
  warn "skipped (not signed in)"
fi

section "Secrets and parameters (names and key prefixes only, values are never printed)"
if have dotnet && [ -d "$APPHOST" ]; then
  stripe_line="$(dotnet user-secrets list --project "$APPHOST" 2>/dev/null | grep '^Parameters:StripeApiKey' | head -1)"
  if [ -n "$stripe_line" ]; then
    value="${stripe_line#*= }"
    case "$value" in
      sk_test_*) pass "Parameters:StripeApiKey is set and is a TEST key (sk_test_...)" ;;
      sk_live_*) fail "Parameters:StripeApiKey is a LIVE key: this project must only use Stripe test mode" ;;
      *)         warn "Parameters:StripeApiKey is set but does not start with sk_test_" ;;
    esac
  else
    warn "Parameters:StripeApiKey not in AppHost user-secrets (azd will prompt for it on first provision)"
  fi
fi

# Front end: the publishable key is baked into the browser bundle, so only its prefix class is checked.
check_frontend_stripe_key() {
  local envfile="$1" fe_line fe_value
  [ -f "$envfile" ] || return 0
  fe_line="$(grep '^VITE_STRIPE_PUBLISHABLE_KEY=' "$envfile" 2>/dev/null | head -1)"
  fe_value="${fe_line#*=}"
  fe_value="$(printf '%s' "$fe_value" | tr -d '\r"'"'"' ')"
  case "$fe_value" in
    pk_test_*) pass "frontend .env.local VITE_STRIPE_PUBLISHABLE_KEY is a TEST key (pk_test_...)" ;;
    pk_live_*) fail "frontend .env.local VITE_STRIPE_PUBLISHABLE_KEY is a LIVE key: test mode only" ;;
    *)         warn "frontend .env.local VITE_STRIPE_PUBLISHABLE_KEY is missing or does not start with pk_test_" ;;
  esac
}
check_frontend_stripe_key "$ROOT/frontend/GameStore.Frontend/.env.local"

section "Repository"
if git -C "$ROOT" diff --quiet && git -C "$ROOT" diff --cached --quiet; then
  pass "working tree has no uncommitted changes to tracked files"
else
  warn "uncommitted changes in tracked files (azd deploys what is on disk)"
fi
if git -C "$ROOT" check-ignore -q backend/.azure/config.json 2>/dev/null; then
  pass "backend/.azure/ is git-ignored (azd environment values stay out of git)"
else
  fail "backend/.azure/ is NOT git-ignored: azd would store environment values in a tracked folder"
fi
# git ls-files compares names case-sensitively; [ -f ] does not on macOS
if git -C "$ROOT" ls-files --error-unmatch backend/src/GameStore.AppHost/bicep/frontdoor.bicep >/dev/null 2>&1; then
  pass "bicep file name matches the AppHost reference (frontdoor.bicep)"
else
  fail "backend/src/GameStore.AppHost/bicep/frontdoor.bicep is missing or has a different case"
fi
if grep -q "\[ENTRA\|\[STRIPE\|\[BACKEND API URL" "$APPHOST/appsettings.json" 2>/dev/null; then
  warn "appsettings.json still has [... HERE] placeholders: provide real values when azd prompts (do not commit them)"
fi

if [ "$FULL" -eq 1 ]; then
  section "Build and unit tests (--full)"
  if (cd "$ROOT" && dotnet build backend/Backend.sln >/dev/null 2>&1); then
    pass "dotnet build backend/Backend.sln"
  else
    fail "dotnet build backend/Backend.sln failed (run it directly to see the errors)"
  fi
  if (cd "$ROOT" && dotnet test backend/tests/GameStore.Api.UnitTests >/dev/null 2>&1); then
    pass "unit tests pass"
  else
    fail "unit tests failed (run them directly to see the errors)"
  fi
fi

printf '\nSummary: %d ok, %d warnings, %d failed\n' "$PASS" "$WARN" "$FAIL"
if [ "$FAIL" -gt 0 ]; then
  echo "Not ready to deploy: fix the FAIL lines above."
  exit 1
fi
echo "Ready for the next step in docs/deployment.md (warnings are informational)."
exit 0
