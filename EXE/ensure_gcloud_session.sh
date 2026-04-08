#!/usr/bin/env bash
# 명령: Secret Manager / PAT 쓰기 전에 gcloud 계정·프로젝트가 실제로 잡혀 있는지 검증·보정

set -euo pipefail

PROJECT_ID="${PROJECT_ID:-project-6868f681-721e-4d33-b59}"
export PROJECT_ID
export GOOGLE_CLOUD_PROJECT="${GOOGLE_CLOUD_PROJECT:-$PROJECT_ID}"

if ! command -v gcloud >/dev/null 2>&1; then
  echo "[ensure_gcloud_session] gcloud CLI 없음. Google Cloud SDK 설치 후 재시도."
  exit 1
fi

_acct="$(gcloud auth list --filter=status:ACTIVE --format="value(account)" 2>/dev/null | head -1)"
if [[ -z "$_acct" ]]; then
  echo "[ensure_gcloud_session] Credentialed 계정 없음 (gcloud auth list 가 비어 있음)."
  echo "  이 세션에서 순서대로 실행:"
  echo "    gcloud auth login --no-launch-browser"
  echo "    gcloud config set project ${PROJECT_ID}"
  echo "  서비스 계정이면:"
  echo "    gcloud auth activate-service-account --key-file=KEY.json"
  exit 1
fi

_cur="$(gcloud config get-value project 2>/dev/null || true)"
if [[ -z "$_cur" || "$_cur" == "(unset)" ]]; then
  gcloud config set project "${PROJECT_ID}"
fi

echo "[ensure_gcloud_session] OK account=${_acct} project=$(gcloud config get-value project)"
