#!/usr/bin/env bash
# ReportMaker·NewMCP 등 저장소 루트에 EXE/ios_push_with_pat.sh 가 있을 때 사용.
#   chmod +x EXE/vm_ios_push.sh && ./EXE/vm_ios_push.sh
# VM 기본 경로(/home/ubuntu/ReportMaker)가 없으면, 이 스크립트 위치(…/EXE)의 상위 폴더를 루트로 씀.
# 다른 경로면: REPO_ROOT=/절대/경로 ./EXE/vm_ios_push.sh

set -euo pipefail

_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
_FROM_SCRIPT_ROOT="$(cd "$_SCRIPT_DIR/.." && pwd)"

if [[ -n "${REPO_ROOT:-}" ]]; then
  :
elif [[ -f "$_FROM_SCRIPT_ROOT/EXE/ios_push_with_pat.sh" ]]; then
  REPO_ROOT="$_FROM_SCRIPT_ROOT"
elif [[ -f "/home/ubuntu/ReportMaker/EXE/ios_push_with_pat.sh" ]]; then
  REPO_ROOT="/home/ubuntu/ReportMaker"
else
  echo "[vm_ios_push] EXE/ios_push_with_pat.sh 없음. 저장소 루트에서 실행하거나: export REPO_ROOT=/클론경로"
  exit 1
fi

cd "$REPO_ROOT" || {
  echo "[vm_ios_push] cd 실패: $REPO_ROOT"
  exit 1
}

if [[ ! -f EXE/ios_push_with_pat.sh ]]; then
  echo "[vm_ios_push] EXE/ios_push_with_pat.sh 없음 (REPO_ROOT=$REPO_ROOT)."
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
