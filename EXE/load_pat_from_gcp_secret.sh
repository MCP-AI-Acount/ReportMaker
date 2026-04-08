#!/usr/bin/env bash
# 명령: GCP Secret Manager 에서 GitHub PAT를 읽어 env로 주입

set -euo pipefail

if ! command -v gcloud >/dev/null 2>&1; then
  echo "[load_pat_from_gcp_secret] gcloud not found"
  exit 1
fi

PROJECT_ID="${PROJECT_ID:-$(gcloud config get-value project 2>/dev/null || true)}"
GITHUB_TOKEN_SECRET_NAME="${GITHUB_TOKEN_SECRET_NAME:-github-token}"
GH_TOKEN_SECRET_NAME="${GH_TOKEN_SECRET_NAME:-gh-token}"

if [[ -z "$PROJECT_ID" ]]; then
  echo "[load_pat_from_gcp_secret] PROJECT_ID is empty"
  exit 1
fi

github_token_value=""
gh_token_value=""

if github_token_value="$(gcloud secrets versions access latest --secret="$GITHUB_TOKEN_SECRET_NAME" --project="$PROJECT_ID" 2>/dev/null)"; then
  export GITHUB_TOKEN="$github_token_value"
fi

if gh_token_value="$(gcloud secrets versions access latest --secret="$GH_TOKEN_SECRET_NAME" --project="$PROJECT_ID" 2>/dev/null)"; then
  export GH_TOKEN="$gh_token_value"
fi

# GH_TOKEN 미존재 시 GITHUB_TOKEN 값으로 채움
if [[ -z "${GH_TOKEN:-}" && -n "${GITHUB_TOKEN:-}" ]]; then
  export GH_TOKEN="$GITHUB_TOKEN"
fi

if [[ -z "${GITHUB_TOKEN:-}" && -n "${GH_TOKEN:-}" ]]; then
  export GITHUB_TOKEN="$GH_TOKEN"
fi

if [[ -z "${GITHUB_TOKEN:-}" ]]; then
  echo "[load_pat_from_gcp_secret] token not loaded from secret manager"
  exit 1
fi

echo "[load_pat_from_gcp_secret] token loaded from secret manager"
