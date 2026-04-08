#!/usr/bin/env bash
# 명령: 한 번에 커밋/푸시/PR 생성 (아이폰 원커맨드용)

set -euo pipefail

REPO_DIR="${REPO_DIR:-$(pwd)}"
REMOTE_NAME="${REMOTE_NAME:-origin}"
BASE_BRANCH="${BASE_BRANCH:-main}"
SECRETS_FILE="${SECRETS_FILE:-$HOME/.config/agent-secrets.env}"
ALT_SECRETS_FILE="${ALT_SECRETS_FILE:-$REPO_DIR/temp/agent-secrets.env}"
GCP_SECRET_LOADER="${GCP_SECRET_LOADER:-$REPO_DIR/EXE/load_pat_from_gcp_secret.sh}"
COMMIT_MESSAGE="${1:-}"
PR_TITLE="${2:-}"

if [[ -z "$COMMIT_MESSAGE" ]]; then
  echo "usage: $0 <commit-message> [pr-title]"
  exit 1
fi

cd "$REPO_DIR"

if [[ ! -d .git ]]; then
  echo "git 저장소가 아닙니다: $REPO_DIR"
  exit 1
fi

if [[ -f "$SECRETS_FILE" ]]; then
  set -a
  # shellcheck disable=SC1090
  source "$SECRETS_FILE"
  set +a
elif [[ -f "$ALT_SECRETS_FILE" ]]; then
  set -a
  # shellcheck disable=SC1090
  source "$ALT_SECRETS_FILE"
  set +a
fi

if [[ -z "${GITHUB_TOKEN:-}" && -x "$GCP_SECRET_LOADER" ]]; then
  bash "$GCP_SECRET_LOADER" >/dev/null 2>&1 || true
fi

if [[ -z "$(git status --porcelain)" ]]; then
  echo "변경사항이 없습니다."
  exit 0
fi

CURRENT_BRANCH="$(git branch --show-current)"
if [[ -z "$CURRENT_BRANCH" || "$CURRENT_BRANCH" == "$BASE_BRANCH" ]]; then
  SAFE_MESSAGE="$(echo "$COMMIT_MESSAGE" | tr ' ' '-' | tr -cd '[:alnum:]-' | tr '[:upper:]' '[:lower:]')"
  SAFE_MESSAGE="${SAFE_MESSAGE:0:24}"
  CURRENT_BRANCH="ios/${SAFE_MESSAGE}-$(date +%m%d%H%M)"
  git checkout -b "$CURRENT_BRANCH"
fi

git add -A
if [[ -z "$(git diff --cached --name-only)" ]]; then
  echo "스테이징된 변경사항이 없습니다."
  exit 0
fi

git commit -m "$COMMIT_MESSAGE"

if [[ -n "${GITHUB_TOKEN:-}" ]]; then
  AUTH_HEADER="$(printf "x-access-token:%s" "$GITHUB_TOKEN" | base64)"
  git -c "http.https://github.com/.extraheader=AUTHORIZATION: basic ${AUTH_HEADER}" \
    push -u "$REMOTE_NAME" "$CURRENT_BRANCH"
else
  echo "GITHUB_TOKEN 없음: push 중단"
  exit 1
fi

if [[ -z "$PR_TITLE" ]]; then
  PR_TITLE="$COMMIT_MESSAGE"
fi

if ! gh pr view "$CURRENT_BRANCH" >/dev/null 2>&1; then
  gh pr create \
    --base "$BASE_BRANCH" \
    --head "$CURRENT_BRANCH" \
    --title "$PR_TITLE" \
    --body "자동 생성 PR (one_command_git_flow.sh)"
fi
