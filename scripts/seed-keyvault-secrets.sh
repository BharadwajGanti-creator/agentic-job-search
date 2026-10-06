#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(dirname "$0")"
source "$SCRIPT_DIR/.secrets.env"
KV_NAME="devops-track-kv2026"
RESUME_JSON="$(cat "$SCRIPT_DIR/resume_profile.local.json")"

az keyvault secret set --vault-name "$KV_NAME" --name "GeminiApiKey" --value "$GEMINI_API_KEY" >/dev/null
az keyvault secret set --vault-name "$KV_NAME" --name "JoobleApiKey" --value "$JOOBLE_API_KEY" >/dev/null
az keyvault secret set --vault-name "$KV_NAME" --name "ResumeProfileJson" --value "$RESUME_JSON" >/dev/null
echo "Secrets seeded into $KV_NAME"
