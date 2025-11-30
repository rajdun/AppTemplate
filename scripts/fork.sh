#!/bin/bash

# ==============================================================================
# TEMPLATE REPOSITORY CONFIGURATION (AppTemplate)
# ==============================================================================
TEMPLATE_SSH="git@github.com:rajdun/AppTemplate.git"
TEMPLATE_HTTPS="https://github.com/rajdun/AppTemplate.git"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# ==============================================================================
# SAFETY CHECKS
# ==============================================================================
set -e # Stop script on first error

error() {
    echo -e "${RED}[ERROR] $1${NC}"
    exit 1
}

log() {
    echo -e "${GREEN}[SETUP] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[INFO] $1${NC}"
}

# 1. Check input parameter
if [ -z "$1" ]; then
    error "No repository URL provided.\nUsage: ./fork.sh <NEW_REPOSITORY_URL>"
fi

NEW_REPO_URL="$1"

# 2. Detect protocol (SSH vs HTTPS)
# Check if the link starts with "git@" (SSH) or "https://" (HTTPS)
if [[ "$NEW_REPO_URL" == git@* ]]; then
    PROTOCOL="SSH"
    TEMPLATE_URL="$TEMPLATE_SSH"
    warn "SSH link detected. Using SSH template."
elif [[ "$NEW_REPO_URL" == https://* ]]; then
    PROTOCOL="HTTPS"
    TEMPLATE_URL="$TEMPLATE_HTTPS"
    warn "HTTPS link detected. Using HTTPS template."
else
    error "Unknown link format. URL must start with 'https://' or 'git@'."
fi

# 3. Extract directory name from the link
PROJECT_NAME=$(basename "$NEW_REPO_URL" .git)

# 4. Check for name conflicts
if [ -d "$PROJECT_NAME" ]; then
    error "Directory '$PROJECT_NAME' already exists. Remove or move it."
fi

# ==============================================================================
# MAIN LOGIC
# ==============================================================================

log "Step 1: Cloning template ($PROTOCOL)..."
git clone "$TEMPLATE_URL" "$PROJECT_NAME"

log "Step 2: Configuring repository..."
cd "$PROJECT_NAME"

# Rename origin -> upstream
git remote rename origin upstream

# Add new address as origin
git remote add origin "$NEW_REPO_URL"

log "Step 3: Pushing code..."
# Detect branch name (main/master)
CURRENT_BRANCH=$(git branch --show-current)

# Push to new repository
git push -u origin "$CURRENT_BRANCH"

echo ""
echo "----------------------------------------------------------------"
echo -e "${GREEN}SUCCESS! Project '$PROJECT_NAME' configured (${PROTOCOL}).${NC}"
echo "----------------------------------------------------------------"
echo "Remotes:"
git remote -v
echo "----------------------------------------------------------------"
echo "You can enter the directory:"
echo "cd $PROJECT_NAME"
