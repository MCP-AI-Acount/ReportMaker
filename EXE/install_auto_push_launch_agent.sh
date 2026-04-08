#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="${REPO_DIR:-/Users/Windows/Documents/Task/Unity/ReportMaker}"
LAUNCH_AGENTS_DIR="$HOME/Library/LaunchAgents"
PLIST_PATH="$LAUNCH_AGENTS_DIR/com.reportmaker.autopush.pr.plist"
RUN_SCRIPT="$REPO_DIR/EXE/auto_push_pr_on_idle.sh"

mkdir -p "$LAUNCH_AGENTS_DIR"

if [[ ! -x "$RUN_SCRIPT" ]]; then
  echo "실행 스크립트 없음: $RUN_SCRIPT"
  exit 1
fi

cat > "$PLIST_PATH" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>Label</key><string>com.reportmaker.autopush.pr</string>
  <key>ProgramArguments</key>
  <array>
    <string>/bin/bash</string>
    <string>$RUN_SCRIPT</string>
  </array>
  <key>RunAtLoad</key><true/>
  <key>KeepAlive</key><true/>
  <key>StandardOutPath</key><string>/tmp/reportmaker-autopush.log</string>
  <key>StandardErrorPath</key><string>/tmp/reportmaker-autopush.err</string>
</dict>
</plist>
PLIST

launchctl unload "$PLIST_PATH" >/dev/null 2>&1 || true
launchctl load "$PLIST_PATH"

echo "launch agent installed: $PLIST_PATH"
