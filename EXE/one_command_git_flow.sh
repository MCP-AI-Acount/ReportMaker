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
  # 실패 로그는 보이게(디버그). 성공 시에만 조용히 해도 됨
  bash "$GCP_SECRET_LOADER" || true
fi

push_with_pat() {
  local br="$1"
  if [[ -z "${GITHUB_TOKEN:-}" ]]; then
    echo "GITHUB_TOKEN 없음: push 중단 (load_pat 또는 agent-secrets.env 확인)"
    exit 1
  fi
  local auth_header
  auth_header="$(printf "x-access-token:%s" "$GITHUB_TOKEN" | base64)"
  git -c "http.https://github.com/.extraheader=AUTHORIZATION: basic ${auth_header}" \
    push -u "$REMOTE_NAME" "$br"
}

CURRENT_BRANCH="$(git branch --show-current)"

# 작업 트리 깨끗 + 로컬만 앞선 커밋 있음 → 커밋 없이 push만
if [[ -z "$(git status --porcelain)" ]]; then
  AHEAD=0
  _sb="$(git status -sb | head -1)"
  if [[ "$_sb" =~ \[ahead\ ([0-9]+)\] ]]; then
    AHEAD="${BASH_REMATCH[1]}"
  elif git rev-parse --verify '@{u}' >/dev/null 2>&1; then
    AHEAD=$(git rev-list --count '@{u}..HEAD' 2>/dev/null || echo 0)
  elif git show-ref --verify --quiet "refs/remotes/${REMOTE_NAME}/${CURRENT_BRANCH}"; then
    AHEAD=$(git rev-list --count "${REMOTE_NAME}/${CURRENT_BRANCH}..HEAD" 2>/dev/null || echo 0)
  elif git show-ref --verify --quiet "refs/remotes/${REMOTE_NAME}/${BASE_BRANCH}"; then
    AHEAD=$(git rev-list --count "${REMOTE_NAME}/${BASE_BRANCH}..HEAD" 2>/dev/null || echo 0)
  fi
  if [[ "${AHEAD:-0}" -gt 0 ]]; then
    echo "[one_command] 작업 트리 깨끗함, 로컬만 ${AHEAD}커밋 앞섬 → push만 시도"
    push_with_pat "$CURRENT_BRANCH"
    if [[ -z "$PR_TITLE" ]]; then
      PR_TITLE="$COMMIT_MESSAGE"
    fi
    if command -v gh >/dev/null 2>&1 && ! gh pr view "$CURRENT_BRANCH" >/dev/null 2>&1; then
      gh pr create \
        --base "$BASE_BRANCH" \
        --head "$CURRENT_BRANCH" \
        --title "$PR_TITLE" \
        --body "자동 생성 PR (one_command_git_flow.sh, push-only)" || true
    fi
    exit 0
  fi
  echo "변경사항이 없고, 원격에 올릴 앞선 커밋도 없습니다."
  exit 0
fi
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

push_with_pat "$CURRENT_BRANCH"

if [[ -z "$PR_TITLE" ]]; then
  PR_TITLE="$COMMIT_MESSAGE"
fi

if command -v gh >/dev/null 2>&1; then
  if ! gh pr view "$CURRENT_BRANCH" >/dev/null 2>&1; then
    gh pr create \
      --base "$BASE_BRANCH" \
      --head "$CURRENT_BRANCH" \
      --title "$PR_TITLE" \
      --body "자동 생성 PR (one_command_git_flow.sh)"
  fi
else
  echo "gh 없음: PR 생략"
fi
