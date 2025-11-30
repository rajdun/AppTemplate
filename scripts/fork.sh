#!/bin/bash

# ==============================================================================
# KONFIGURACJA ADRESÓW SZABLONU (AppTemplate)
# ==============================================================================
TEMPLATE_SSH="git@github.com:rajdun/AppTemplate.git"
TEMPLATE_HTTPS="https://github.com/rajdun/AppTemplate.git"

# Kolory
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# ==============================================================================
# ZABEZPIECZENIA
# ==============================================================================
set -e # Zatrzymaj skrypt przy pierwszym błędzie

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

# 1. Sprawdzenie parametru wejściowego
if [ -z "$1" ]; then
    error "Nie podano adresu nowego repozytorium.\nUżycie: ./setup_project.sh <URL_NOWEGO_REPOZYTORIUM>"
fi

NEW_REPO_URL="$1"

# 2. Wykrywanie protokołu (SSH vs HTTPS)
# Sprawdzamy, czy link zaczyna się od "git@" (SSH) czy "https://" (HTTPS)
if [[ "$NEW_REPO_URL" == git@* ]]; then
    PROTOCOL="SSH"
    TEMPLATE_URL="$TEMPLATE_SSH"
    warn "Wykryto link SSH. Używam szablonu po SSH."
elif [[ "$NEW_REPO_URL" == https://* ]]; then
    PROTOCOL="HTTPS"
    TEMPLATE_URL="$TEMPLATE_HTTPS"
    warn "Wykryto link HTTPS. Używam szablonu po HTTPS."
else
    error "Nieznany format linku. Adres musi zaczynać się od 'https://' lub 'git@'."
fi

# 3. Wyciągnięcie nazwy katalogu z linku
PROJECT_NAME=$(basename "$NEW_REPO_URL" .git)

# 4. Sprawdzenie kolizji nazw
if [ -d "$PROJECT_NAME" ]; then
    error "Katalog '$PROJECT_NAME' już istnieje. Usuń go lub przenieś."
fi

# ==============================================================================
# GŁÓWNA LOGIKA
# ==============================================================================

log "Krok 1: Klonowanie szablonu ($PROTOCOL)..."
git clone "$TEMPLATE_URL" "$PROJECT_NAME"

log "Krok 2: Konfiguracja repozytorium..."
cd "$PROJECT_NAME"

# Zmiana nazwy origin -> upstream
git remote rename origin upstream

# Dodanie nowego adresu jako origin
git remote add origin "$NEW_REPO_URL"

log "Krok 3: Wypychanie kodu..."
# Wykrycie nazwy brancha (main/master)
CURRENT_BRANCH=$(git branch --show-current)

# Push do nowego repozytorium
git push -u origin "$CURRENT_BRANCH"

echo ""
echo "----------------------------------------------------------------"
echo -e "${GREEN}SUKCES! Projekt '$PROJECT_NAME' skonfigurowany (${PROTOCOL}).${NC}"
echo "----------------------------------------------------------------"
echo "Remotes:"
git remote -v
echo "----------------------------------------------------------------"
echo "Możesz wejść do katalogu:"
echo "cd $PROJECT_NAME"