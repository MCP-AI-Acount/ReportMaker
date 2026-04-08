#!/usr/bin/env bash
# 명령: GCP Secret Manager 에서 GitHub PAT를 읽어 env로 주입

set -euo pipefail

# 우선순위: PROJECT_ID > GOOGLE_CLOUD_PROJECT > gcloud 현재 프로젝트 > (선택) 기본값
# VM/Cloud Run에서 GOOGLE_CLOUD_PROJECT가 자주 잡힘
DEFAULT_GCP_PROJECT_ID="${DEFAULT_GCP_PROJECT_ID:-project-6868f681-721e-4d33-b59}"

if ! command -v gcloud >/dev/null 2>&1; then
  echo "[load_pat_from_gcp_secret] gcloud 없음. 설치하거나 PAT는 ~/.config/agent-secrets.env 등으로 주입."
  exit 1
fi

PROJECT_ID="${PROJECT_ID:-}"
if [[ -z "$PROJECT_ID" && -n "${GOOGLE_CLOUD_PROJECT:-}" ]]; then
  PROJECT_ID="$GOOGLE_CLOUD_PROJECT"
fi
if [[ -z "$PROJECT_ID" ]]; then
  PROJECT_ID="$(gcloud config get-value project 2>/dev/null || true)"
fi
if [[ -z "$PROJECT_ID" || "$PROJECT_ID" == "(unset)" ]]; then
  PROJECT_ID="$DEFAULT_GCP_PROJECT_ID"
fi

export PROJECT_ID

GITHUB_TOKEN_SECRET_NAME="${GITHUB_TOKEN_SECRET_NAME:-github-token}"
GH_TOKEN_SECRET_NAME="${GH_TOKEN_SECRET_NAME:-gh-token}"

_active_account="$(gcloud config get-value account 2>/dev/null || true)"
if [[ -z "$_active_account" || "$_active_account" == "(unset)" ]]; then
  echo "[load_pat_from_gcp_secret] gcloud 활성 계정 없음. 한 번 실행:"
  echo "  gcloud auth login"
  echo "  gcloud config set project ${PROJECT_ID}"
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

if [[ -z "${GH_TOKEN:-}" && -n "${GITHUB_TOKEN:-}" ]]; then
  export GH_TOKEN="$GITHUB_TOKEN"
fi

if [[ -z "${GITHUB_TOKEN:-}" && -n "${GH_TOKEN:-}" ]]; then
  export GITHUB_TOKEN="$GH_TOKEN"
fi

if [[ -z "${GITHUB_TOKEN:-}" ]]; then
  echo "[load_pat_from_gcp_secret] token not loaded from Secret Manager"
  echo "[load_pat_from_gcp_secret] 병목은 보통 gcloud 로그인 없음 또는 IAM(secretAccessor) 부족 (프로젝트=${PROJECT_ID}, secret=${GITHUB_TOKEN_SECRET_NAME})"
  echo "  gcloud auth login"
  echo "  gcloud config set project ${PROJECT_ID}"
  echo "  gcloud secrets list --project=${PROJECT_ID}"
  echo "  IAM: roles/secretmanager.secretAccessor"
  exit 1
fi

echo "[load_pat_from_gcp_secret] OK (project=${PROJECT_ID})"
