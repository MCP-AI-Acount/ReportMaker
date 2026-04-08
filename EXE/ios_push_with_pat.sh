#!/usr/bin/env bash
# 명령: 아이폰/클라우드용 단일 진입 — ensure → one_command (내부에서 load_pat)
# load_pat / one_command 를 따로 두 번 부르지 말 것 (세션·버전 꼬임 방지)

set -euo pipefail

REPO_DIR="${REPO_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
export REPO_DIR
cd "$REPO_DIR"
export PROJECT_ID="${PROJECT_ID:-project-6868f681-721e-4d33-b59}"
export GOOGLE_CLOUD_PROJECT="${GOOGLE_CLOUD_PROJECT:-$PROJECT_ID}"

exec bash "$REPO_DIR/EXE/one_command_git_flow.sh" "${1:-chore: ios quick sync}" "${2:-}"
