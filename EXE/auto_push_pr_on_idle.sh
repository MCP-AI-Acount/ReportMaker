#!/usr/bin/env bash
# 명령: 트리거 없이 변경 감지 후 자동 커밋/푸시/PR 실행

set -euo pipefail

REPO_DIR="${REPO_DIR:-/Users/Windows/Documents/Task/Unity/ReportMaker}"
IDLE_THRESHOLD_SECONDS="${IDLE_THRESHOLD_SECONDS:-45}"
POLL_SECONDS="${POLL_SECONDS:-15}"
REMOTE_NAME="${REMOTE_NAME:-origin}"
BASE_BRANCH="${BASE_BRANCH:-main}"
COMMIT_PREFIX="${COMMIT_PREFIX:-auto: reportmaker sync}"

FLOW_SCRIPT="${REPO_DIR}/EXE/one_command_git_flow.sh"

cd "$REPO_DIR"

if [[ ! -x "$FLOW_SCRIPT" ]]; then
  echo "실행 스크립트 없음: $FLOW_SCRIPT"
  exit 1
fi

echo "[auto_push_pr_on_idle] started: repo=$REPO_DIR poll=${POLL_SECONDS}s idle=${IDLE_THRESHOLD_SECONDS}s"

while true; do
  if [[ -n "$(git status --porcelain)" ]]; then
    idle_hex="$(ioreg -c IOHIDSystem | awk '/HIDIdleTime/ {print $NF; exit}')"
    idle_ns=$((idle_hex))
    idle_sec=$((idle_ns / 1000000000))

    if (( idle_sec >= IDLE_THRESHOLD_SECONDS )); then
      export REPO_DIR REMOTE_NAME BASE_BRANCH
      commit_msg="${COMMIT_PREFIX} $(date '+%Y-%m-%d %H:%M:%S')"
      pr_title="${commit_msg}"
      bash "$FLOW_SCRIPT" "$commit_msg" "$pr_title" || true
    fi
  fi
  sleep "$POLL_SECONDS"
done
