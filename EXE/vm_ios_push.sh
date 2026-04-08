#!/usr/bin/env bash
# VM(우분투)에서만 사용: 홈에 복사하거나 저장소 EXE 안에서 실행.
#   chmod +x vm_ios_push.sh
#   ./vm_ios_push.sh
#   ./vm_ios_push.sh "커밋메시지" "PR제목"
# REPO_ROOT가 다르면: export REPO_ROOT=/실제/ReportMaker/경로

set -euo pipefail

REPO_ROOT="${REPO_ROOT:-/home/ubuntu/ReportMaker}"
cd "$REPO_ROOT" || {
  echo "[vm_ios_push] cd 실패: $REPO_ROOT"
  exit 1
}

if [[ ! -f EXE/ios_push_with_pat.sh ]]; then
  echo "[vm_ios_push] EXE/ios_push_with_pat.sh 없음. ReportMaker 루트인지 확인하고 git pull 하세요."
  exit 1
fi

git pull

export REPO_DIR="$(pwd)"
export PROJECT_ID="${PROJECT_ID:-project-6868f681-721e-4d33-b59}"
export GOOGLE_CLOUD_PROJECT="${GOOGLE_CLOUD_PROJECT:-$PROJECT_ID}"

if [[ -z "$(gcloud auth list --filter=status:ACTIVE --format="value(account)" 2>/dev/null | head -1)" ]]; then
  gcloud auth login --no-launch-browser
fi
gcloud config set project "${PROJECT_ID}"

MSG="${1:-chore: ios quick sync}"
PR_TITLE="${2:-$MSG}"
exec bash EXE/ios_push_with_pat.sh "$MSG" "$PR_TITLE"
